namespace DistributedLocalSystem.Application.Net.Internal;

/// <summary>Классификация исключений конфликта режима хоста в LAN.</summary>
internal static class HostCollisionExceptionClassifier
{
    public static bool IsAnotherHostPresent(string message) =>
        message.Contains("another host", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Net: another host", StringComparison.OrdinalIgnoreCase);
}
