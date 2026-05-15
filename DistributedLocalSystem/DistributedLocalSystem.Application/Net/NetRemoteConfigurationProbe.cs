using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net;

/// <summary>HTTP-запрос конфигурации удалённого узла по LAN-порту.</summary>
internal static class NetRemoteConfigurationProbe
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static async Task<Outcome<DiscoveryOptions>> FetchAsync(
        HttpClient http,
        IPAddress remoteAddress,
        int lanPort,
        string canonicalIpForMessages,
        CancellationToken cancellationToken
    )
    {
        Uri uri = BuildConfigurationUri(remoteAddress, lanPort);

        try
        {
            using HttpResponseMessage response = await http.GetAsync(uri, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Outcome<DiscoveryOptions>.Fail(
                    NetFlowErrorCodes.RemoteHostUnreachable,
                    $"Сервер по адресу {canonicalIpForMessages} недоступен (HTTP {(int)response.StatusCode}). "
                        + "Проверьте IP и что на том компьютере запущена та же программа."
                );
            }

            DiscoveryOptions? remoteCfg = await response
                .Content.ReadFromJsonAsync<DiscoveryOptions>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (remoteCfg is null)
            {
                return Outcome<DiscoveryOptions>.Fail(
                    NetFlowErrorCodes.RemoteHostUnreachable,
                    "Пустой ответ конфигурации с удалённого узла."
                );
            }

            return Outcome<DiscoveryOptions>.Ok(remoteCfg);
        }
        catch (OperationCanceledException)
        {
            return Outcome<DiscoveryOptions>.Fail(
                NetFlowErrorCodes.OperationCancelled,
                "Операция отменена."
            );
        }
        catch (HttpRequestException)
        {
            return Outcome<DiscoveryOptions>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                $"Не удалось связаться с {canonicalIpForMessages}:{lanPort}. "
                    + "Убедитесь, что узел в сети и порт LAN совпадает с вашим."
            );
        }
        catch (JsonException)
        {
            return Outcome<DiscoveryOptions>.Fail(
                NetFlowErrorCodes.RemoteHostUnreachable,
                "Удалённый узел ответил, но конфигурация не распознана. Версия приложения может отличаться."
            );
        }
        catch (Exception)
        {
            return Outcome<DiscoveryOptions>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }

    private static Uri BuildConfigurationUri(IPAddress addr, int lanPort)
    {
        string host =
            addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                ? $"[{addr}]"
                : addr.ToString();
        return new Uri($"http://{host}:{lanPort}/api/net/configuration");
    }
}
