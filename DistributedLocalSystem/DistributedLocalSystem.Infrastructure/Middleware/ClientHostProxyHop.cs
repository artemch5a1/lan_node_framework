namespace DistributedLocalSystem.Infrastructure.Middleware;

/// <summary>Счётчик hop проксирования в HTTP-заголовке (один запрос, без realtime).</summary>
public static class ClientHostProxyHop
{
    public const string HeaderName = "X-DLS-Proxy-Hop";

    /// <summary>Отклонять запрос, если входящий hop &gt;= этого значения (1 = не более одного проксирования).</summary>
    public const int DefaultMaxIncomingHop = 1;

    public static int ReadIncomingHop(Microsoft.AspNetCore.Http.IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(HeaderName, out Microsoft.Extensions.Primitives.StringValues raw))
            return 0;

        string? first = raw.FirstOrDefault();
        return int.TryParse(first, out int hop) && hop > 0 ? hop : 0;
    }

    public static bool ShouldRejectProxy(int incomingHop, int maxIncomingHopBeforeReject) =>
        incomingHop >= maxIncomingHopBeforeReject;

    public static int NextOutgoingHop(int incomingHop) => incomingHop + 1;
}
