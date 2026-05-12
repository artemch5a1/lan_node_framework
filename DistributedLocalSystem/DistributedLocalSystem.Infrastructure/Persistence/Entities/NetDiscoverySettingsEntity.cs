using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DistributedLocalSystem.Infrastructure.Persistence.Entities;

[Table("net_discovery_settings")]
public sealed class NetDiscoverySettingsEntity
{
    public const int SingleRowId = 1;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = SingleRowId;

    [MaxLength(32)]
    public string Role { get; set; } = "host";

    [MaxLength(256)]
    public string AppId { get; set; } = "";

    [MaxLength(48)]
    public string ProductSlug { get; set; } = "";

    [MaxLength(48)]
    public string InstanceSlug { get; set; } = "";

    [MaxLength(64)]
    public string InstanceGuid { get; set; } = "";

    [MaxLength(45)]
    public string? RemoteHostIp { get; set; }

    public int UdpPort { get; set; }

    public int LanPort { get; set; }

    public int BeaconIntervalMs { get; set; }

    public int DiscoveryTimeoutMs { get; set; }

    public int ProtocolVersion { get; set; }
}
