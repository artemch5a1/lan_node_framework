using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public interface IChangeNetConfigurationUseCase
{
    Task<Outcome<NetConfigurationState>> ExecuteAsync(
        NetConfigurationState next,
        CancellationToken cancellationToken
    );
}
