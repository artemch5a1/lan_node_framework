using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public sealed class GetLanPeersUseCase : IGetLanPeersUseCase
{
    private readonly INetLanOrchestrator _orchestrator;

    public GetLanPeersUseCase(INetLanOrchestrator orchestrator) => _orchestrator = orchestrator;

    public Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ExecuteAsync(
        CancellationToken cancellationToken
    ) => _orchestrator.ListLanNodesAsync(cancellationToken);
}
