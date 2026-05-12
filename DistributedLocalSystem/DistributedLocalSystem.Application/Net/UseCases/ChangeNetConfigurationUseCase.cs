using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public sealed class ChangeNetConfigurationUseCase : IChangeNetConfigurationUseCase
{
    private readonly INetLanOrchestrator _orchestrator;

    public ChangeNetConfigurationUseCase(INetLanOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public Task<Outcome<NetConfigurationState>> ExecuteAsync(
        NetConfigurationState next,
        CancellationToken cancellationToken
    ) => _orchestrator.ApplyConfigurationStateAsync(next, cancellationToken);
}
