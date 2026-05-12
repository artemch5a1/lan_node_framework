using DistributedLocalSystem.Core.Abstractions;
using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public sealed class GetNetConfigurationUseCase : IGetNetConfigurationUseCase
{
    private readonly INetLanOrchestrator _orchestrator;

    public GetNetConfigurationUseCase(INetLanOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public Outcome<NetConfigurationState> Execute() => _orchestrator.GetConfigurationState();
}
