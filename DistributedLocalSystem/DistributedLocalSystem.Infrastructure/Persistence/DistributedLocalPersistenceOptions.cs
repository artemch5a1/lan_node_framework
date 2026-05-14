namespace DistributedLocalSystem.Infrastructure.Persistence;

public sealed class DistributedLocalPersistenceOptions
{
    public const string SectionName = "DistributedLocal";

    /// <summary>Путь к файлу SQLite: абсолютный или относительно <see cref="AppContext.BaseDirectory"/>.</summary>
    public string? DataSource { get; set; }
}
