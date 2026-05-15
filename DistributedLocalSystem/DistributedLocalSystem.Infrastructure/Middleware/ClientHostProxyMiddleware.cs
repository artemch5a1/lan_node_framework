using System.Net.Http.Headers;
using System.Text.Json;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Infrastructure.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace DistributedLocalSystem.Infrastructure.Middleware;

/// <summary>
/// В режиме клиента с найденным хостом пересылает HTTP на LAN-хост, кроме служебных путей (health, net API).
/// </summary>
public sealed class ClientHostProxyMiddleware(
    RequestDelegate next,
    IHttpClientFactory httpClientFactory,
    ILogger<ClientHostProxyMiddleware> logger,
    IOptions<ClientHostProxyOptions> options
)
{
    private static readonly HashSet<string> HopByHopRequestHeaders = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host",
        "Content-Length",
        "Content-Type",
    };

    private static readonly HashSet<string> HopByHopResponseHeaders = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "Transfer-Encoding",
        "Connection",
    };

    private static readonly HashSet<string> MethodsWithBody = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    private readonly RequestDelegate _next = next;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ClientHostProxyMiddleware> _logger = logger;

    private readonly ClientHostProxyOptions _options = options.Value;

    private static readonly JsonSerializerOptions ProxyChainJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext context, INetDiscoveryRuntime net)
    {
        if (ShouldSkipProxy(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!TryGetRemoteBaseUrl(net, out string? remoteBase) || remoteBase is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        int incomingHop = ClientHostProxyHop.ReadIncomingHop(context.Request.Headers);
        if (ClientHostProxyHop.ShouldRejectProxy(incomingHop, _options.MaxIncomingProxyHop))
        {
            await SendProxyChainRejectedResponseAsync(context).ConfigureAwait(false);
            return;
        }

        await ProxyRequestAsync(context, remoteBase, net, incomingHop).ConfigureAwait(false);
    }

    #region Private Methods

    private bool ShouldSkipProxy(HttpContext context)
    {
        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<NotRedirect>() != null)
            return true;

        string path = context.Request.Path.Value ?? "/";
        if (IsIgnoredPath(path))
            return true;

        return false;
    }

    private bool IsIgnoredPath(string path)
    {
        if (_options.IgnoredPaths.Contains(path))
            return true;

        foreach (string ignoredPath in _options.IgnoredPaths)
        {
            if (path.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (_options.IgnoredPathPatterns.Any())
        {
            foreach (string pattern in _options.IgnoredPathPatterns)
            {
                if (
                    System.Text.RegularExpressions.Regex.IsMatch(
                        path,
                        pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    )
                )
                    return true;
            }
        }

        if (path == "/" || path == string.Empty)
            return !_options.ProxyRootPath;

        return false;
    }

    private static bool TryGetRemoteBaseUrl(INetDiscoveryRuntime net, out string? remoteBase)
    {
        return net.TryGetHostProxyBaseUrl(out remoteBase) && !string.IsNullOrEmpty(remoteBase);
    }

    private async Task ProxyRequestAsync(
        HttpContext context,
        string remoteBase,
        INetDiscoveryRuntime net,
        int incomingHop
    )
    {
        try
        {
            await ForwardToRemoteHostAsync(context, remoteBase, incomingHop).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsTransportError(ex))
        {
            await HandleTransportErrorAsync(context, remoteBase, net, ex).ConfigureAwait(false);
        }
    }

    private async Task ForwardToRemoteHostAsync(
        HttpContext context,
        string remoteBase,
        int incomingHop
    )
    {
        Uri targetUri = BuildTargetUri(context.Request, remoteBase);
        using HttpRequestMessage requestMessage = CreateProxyRequest(
            context.Request,
            targetUri,
            incomingHop
        );

        using HttpClient httpClient = _httpClientFactory.CreateClient("hostProxy");
        using HttpResponseMessage response = await httpClient
            .SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted
            )
            .ConfigureAwait(false);

        await CopyResponseToContextAsync(context, response).ConfigureAwait(false);
    }

    private static HttpRequestMessage CreateProxyRequest(
        HttpRequest request,
        Uri targetUri,
        int incomingHop
    )
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(
            new HttpMethod(request.Method),
            targetUri
        );

        CopyRequestHeaders(request, requestMessage);
        requestMessage.Headers.TryAddWithoutValidation(
            ClientHostProxyHop.HeaderName,
            ClientHostProxyHop.NextOutgoingHop(incomingHop).ToString()
        );

        if (RequestHasBody(request))
        {
            requestMessage.Content = CreateStreamContent(request);
        }

        return requestMessage;
    }

    private static void CopyRequestHeaders(HttpRequest request, HttpRequestMessage requestMessage)
    {
        foreach (KeyValuePair<string, StringValues> header in request.Headers)
        {
            string key = header.Key;
            StringValues value = header.Value;

            if (ShouldSkipRequestHeader(key))
                continue;

            requestMessage.Headers.TryAddWithoutValidation(key, value.ToArray());
        }
    }

    private static bool ShouldSkipRequestHeader(string headerKey)
    {
        return HopByHopRequestHeaders.Contains(headerKey)
            || string.Equals(headerKey, ClientHostProxyHop.HeaderName, StringComparison.OrdinalIgnoreCase)
            || headerKey.StartsWith(':');
    }

    private static StreamContent CreateStreamContent(HttpRequest request)
    {
        StreamContent streamContent = new StreamContent(request.Body);

        if (!string.IsNullOrEmpty(request.ContentType))
        {
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        }

        return streamContent;
    }

    private static bool RequestHasBody(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
            return false;

        return request.ContentLength > 0 || MethodsWithBody.Contains(request.Method);
    }

    private static async Task CopyResponseToContextAsync(
        HttpContext context,
        HttpResponseMessage response
    )
    {
        context.Response.StatusCode = (int)response.StatusCode;

        CopyResponseHeaders(response.Headers, context.Response.Headers);

        if (response.Content != null)
        {
            CopyResponseHeaders(response.Content.Headers, context.Response.Headers);
            await response
                .Content.CopyToAsync(context.Response.Body, context.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    private static void CopyResponseHeaders(HttpHeaders from, IHeaderDictionary to)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in from)
        {
            string key = header.Key;
            IEnumerable<string> values = header.Value;

            if (HopByHopResponseHeaders.Contains(key))
                continue;

            try
            {
                to.Append(key, values.ToArray());
            }
            catch { }
        }
    }

    private static bool IsTransportError(Exception ex)
    {
        return ex is OperationCanceledException or HttpRequestException or IOException;
    }

    private async Task HandleTransportErrorAsync(
        HttpContext context,
        string remoteBase,
        INetDiscoveryRuntime net,
        Exception exception
    )
    {
        if (context.RequestAborted.IsCancellationRequested)
            return;

        bool isHealthCheckSuccessful = await CheckRemoteHealthAsync(
                remoteBase,
                context.RequestAborted
            )
            .ConfigureAwait(false);

        if (!isHealthCheckSuccessful)
        {
            _logger.LogWarning(
                exception,
                "Proxy failed and remote /health unreachable; restarting UDP discovery. Remote base: {RemoteBase}",
                remoteBase
            );
            net.RestartClientDiscoveryAfterRemoteHostFailure();
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Proxy failed but remote /health is OK. Remote base: {RemoteBase}",
                remoteBase
            );
        }

        await SendGatewayErrorResponseAsync(context).ConfigureAwait(false);
    }

    private async Task<bool> CheckRemoteHealthAsync(
        string remoteBase,
        CancellationToken cancellationToken
    )
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("hostProxy");
        Uri healthUri = BuildHealthUri(remoteBase);

        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, healthUri);
            using HttpResponseMessage response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static async Task SendGatewayErrorResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context
            .Response.WriteAsync("Bad gateway: host unreachable.", context.RequestAborted)
            .ConfigureAwait(false);
    }

    private static async Task SendProxyChainRejectedResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status508LoopDetected;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new
        {
            error = new
            {
                code = NetFlowErrorCodes.ProxyChainNotAllowed,
                message =
                    "Запрос уже прошёл через прокси удалённого узла. Цепочка проксирования запрещена. "
                    + "Отключитесь от удалённого хоста и подключитесь напрямую к нужному узлу.",
            },
        };

        await JsonSerializer
            .SerializeAsync(context.Response.Body, body, ProxyChainJsonOptions, context.RequestAborted)
            .ConfigureAwait(false);
    }

    private static Uri BuildTargetUri(HttpRequest request, string remoteBase)
    {
        string relativePath = (request.Path + request.QueryString).ToString();
        if (string.IsNullOrEmpty(relativePath))
            relativePath = "/";

        string baseUri = remoteBase.EndsWith('/') ? remoteBase : remoteBase + "/";
        return new Uri(new Uri(baseUri, UriKind.Absolute), relativePath);
    }

    private static Uri BuildHealthUri(string remoteBase)
    {
        string trimmedBase = remoteBase.TrimEnd('/');
        return new Uri($"{trimmedBase}/health");
    }

    #endregion
}
