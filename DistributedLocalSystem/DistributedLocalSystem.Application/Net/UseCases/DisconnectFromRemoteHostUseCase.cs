using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public sealed class DisconnectFromRemoteHostUseCase : IDisconnectFromRemoteHostUseCase
{
    private readonly INetLanOrchestrator _orchestrator;

    public DisconnectFromRemoteHostUseCase(INetLanOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public Task<Outcome<NetConfigurationState>> ExecuteAsync(CancellationToken cancellationToken) =>
        _orchestrator.DisconnectFromAssignedRemoteAsync(cancellationToken);
}
