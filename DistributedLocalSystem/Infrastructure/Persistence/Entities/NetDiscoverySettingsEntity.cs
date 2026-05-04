using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DistributedLocalSystem.Core.Persistence.Entities;

[Table("net_discovery_settings")]
public sealed class NetDiscoverySettingsEntity
{
    public const int SingleRowId = 1;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = SingleRowId;

    [MaxLength(32)]
    public string Role { get; set; } = "client";

    [MaxLength(256)]
    public string AppId { get; set; } = "";

    public int UdpPort { get; set; }

    public int LanPort { get; set; }

    public int BeaconIntervalMs { get; set; }

    public int DiscoveryTimeoutMs { get; set; }

    public int ProtocolVersion { get; set; }
}
