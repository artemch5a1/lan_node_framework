using Backend.Domain.Common;
using MediatR;

namespace Backend.Application.Abstractions.Mediatr;

public abstract class QueryHandlerBase<TRequest, TResponse>
    : IRequestHandler<TRequest, Result<TResponse>>
    where TRequest : IQuery<TResponse>
    where TResponse : notnull
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await ExecuteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Failure(ex);
        }
    }

    protected abstract Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken
    );
}
