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
}
