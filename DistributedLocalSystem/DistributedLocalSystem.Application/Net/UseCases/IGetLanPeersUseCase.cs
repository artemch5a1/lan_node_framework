using DistributedLocalSystem.Core.Domain.Net;
using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Application.Net.UseCases;

public interface IGetLanPeersUseCase
{
    Task<Outcome<IReadOnlyList<LanNodeDescriptor>>> ExecuteAsync(
        CancellationToken cancellationToken
    );
}
