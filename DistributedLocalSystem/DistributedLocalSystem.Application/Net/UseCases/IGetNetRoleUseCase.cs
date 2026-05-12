using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public interface IGetNetRoleUseCase
{
    Outcome<string> Execute();
}
