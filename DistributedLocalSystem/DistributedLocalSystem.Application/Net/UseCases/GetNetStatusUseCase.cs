using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public sealed class GetNetStatusUseCase : IGetNetStatusUseCase
{
    private readonly INetLanOrchestrator _orchestrator;

    public GetNetStatusUseCase(INetLanOrchestrator orchestrator) => _orchestrator = orchestrator;

    public Outcome<NetRuntimeSnapshot> Execute() => _orchestrator.GetRuntimeSnapshot();
}
