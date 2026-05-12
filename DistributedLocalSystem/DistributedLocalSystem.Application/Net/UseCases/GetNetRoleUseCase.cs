using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public sealed class GetNetRoleUseCase : IGetNetRoleUseCase
{
    private readonly INetLanOrchestrator _orchestrator;

    public GetNetRoleUseCase(INetLanOrchestrator orchestrator) => _orchestrator = orchestrator;

    public Outcome<string> Execute() => _orchestrator.GetConfiguredRoleLabel();
}
