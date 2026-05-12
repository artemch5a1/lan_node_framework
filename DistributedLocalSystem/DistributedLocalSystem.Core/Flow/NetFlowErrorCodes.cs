namespace DistributedLocalSystem.Core.Flow;

/// <summary>Стабильные коды ошибок для HTTP/API.</summary>
public static class NetFlowErrorCodes
{
    public const string Unexpected = "NET_UNEXPECTED";
    public const string HostCollision = "NET_HOST_COLLISION";
    public const string ConfigurationUpdate = "NET_CONFIGURATION_UPDATE";
    public const string ConfigurationReload = "NET_CONFIGURATION_RELOAD";
    public const string LanPeerScan = "NET_LAN_PEER_SCAN";
    public const string OperationCancelled = "NET_OPERATION_CANCELLED";
    public const string AnotherHostAlreadyPresent = "NET_ANOTHER_HOST_PRESENT";
}
