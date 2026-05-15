using DistributedLocalSystem.Application.Net.Internal;
using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.Configuration;

/// <summary>Чтение persisted-конфигурации сети.</summary>
public sealed class NetConfigurationStateReader
{
    private readonly INetDiscoveryRuntime _net;

    public NetConfigurationStateReader(INetDiscoveryRuntime net) => _net = net;

    public Outcome<NetConfigurationState> Read()
    {
        try
        {
            return Outcome<NetConfigurationState>.Ok(
                NetConfigurationState.FromTransport(_net.GetCurrentConfiguration())
            );
        }
        catch (Exception)
        {
            return Outcome<NetConfigurationState>.Fail(
                NetFlowErrorCodes.Unexpected,
                NetApiUserMessages.Unexpected
            );
        }
    }
}
