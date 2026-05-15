namespace DistributedLocalSystem.Infrastructure.Middleware;

public class ClientHostProxyOptions
{
    /// <summary>
    /// Пути, которые следует игнорировать при проксировании (помимо marked endpoints)
    /// </summary>
    public HashSet<string> IgnoredPaths { get; set; } = null!;

    /// <summary>
    /// Разрешить проксирование корневого пути (/)
    /// </summary>
    public bool ProxyRootPath { get; set; } = true;

    /// <summary>
    /// Дополнительные проверки путей (регулярные выражения)
    /// </summary>
    public List<string> IgnoredPathPatterns { get; set; } = new();

    /// <summary>
    /// Отклонять проксирование, если <see cref="ClientHostProxyHop.HeaderName"/> &gt;= этого значения (1 = один hop).
    /// </summary>
    public int MaxIncomingProxyHop { get; set; } = ClientHostProxyHop.DefaultMaxIncomingHop;
}
