using DistributedLocalSystem.Application.Net.Internal;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net.Status;

/// <summary>Чтение снимка состояния сети из runtime.</summary>
public sealed class NetRuntimeSnapshotReader
{
    private readonly INetDiscoveryRuntime _net;

    public NetRuntimeSnapshotReader(INetDiscoveryRuntime net) => _net = net;

    public Outcome<NetRuntimeSnapshot> Read()
    {
        try
        {
            return Outcome<NetRuntimeSnapshot>.Ok(
                NetRuntimeSnapshot.FromTransport(_net.GetStatus())
            );
        }
        catch (Exception)
        {
            return Outcome<NetRuntimeSnapshot>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }

    public Outcome<string> ReadConfiguredRoleLabel()
    {
        try
        {
            NetConfiguredRole role = NetConfiguredRoleExtensions.ParseApiString(
                _net.GetStatus().ConfiguredRole
            );
            return Outcome<string>.Ok(role.GetDescription());
        }
        catch (Exception)
        {
            return Outcome<string>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }
}
