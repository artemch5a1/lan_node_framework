namespace DistributedLocalSystem.Infrastructure.NetDiscovery.Runtime;

/// <summary>Привязка к удалённому LAN-хосту (IP и TCP-порт).</summary>
internal sealed class NetDiscoveryPeerBinding
{
    public string? RemoteHostIp { get; private set; }
    public int? RemoteTcpPort { get; private set; }

    public void Assign(string hostIp, int tcpPort)
    {
        RemoteHostIp = hostIp;
        RemoteTcpPort = tcpPort;
    }

    public void Clear()
    {
        RemoteHostIp = null;
        RemoteTcpPort = null;
    }

    public string? BuildBaseUrl()
    {
        if (string.IsNullOrEmpty(RemoteHostIp) || RemoteTcpPort is null or <= 0)
            return null;
        return $"http://{RemoteHostIp}:{RemoteTcpPort}";
    }
}
