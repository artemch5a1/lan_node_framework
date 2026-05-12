namespace DistributedLocalSystem.Core.NetDiscovery.Model;

public sealed record NetStatusDto(
    string ConfiguredRole,
    NetDiscoveryState State,
    string? ThisHostIp,
    string? RemoteHostIp,
    int? RemoteTcpPort,
    string? RemoteHostBaseUrl,
    int LanPort,
    int UdpPort,
    string AppId,
    string ProductSlug,
    string InstanceSlug,
    string InstanceGuid
);
