using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public interface IGetNetStatusUseCase
{
    Outcome<NetRuntimeSnapshot> Execute();
}
