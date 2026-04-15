using Backend.Application.Abstractions.Repositories.Base;
using Backend.Domain.Common;
using Backend.Domain.Enums;
using Backend.Domain.Exceptions;
using MediatR;

namespace Backend.Application.Abstractions.Mediatr;

public abstract class CommandHandlerAbstract<TCommand, TResult>
    where TCommand : IRequest<Result>
    where TResult : Result
{
    private readonly IUnitOfWork _unitOfWork;

    protected CommandHandlerAbstract(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await ExecuteAsync(request, cancellationToken);

            if (result.IsSuccess)
                await _unitOfWork.CommitAsync(cancellationToken);
            else
                await _unitOfWork.Rollback(cancellationToken);

            return result;
        }
        catch (DomainException ex)
        {
            await _unitOfWork.Rollback(cancellationToken);
            return CreateFailureResult(ex.Message, ApiErrorType.BadRequest);
        }
        catch (Exception ex)
        {
            await _unitOfWork.Rollback(cancellationToken);
            return CreateFailureResult(ex);
        }
    }

    protected abstract Task<TResult> ExecuteAsync(
        TCommand request,
        CancellationToken cancellationToken
    );
    protected abstract TResult CreateFailureResult(string message, ApiErrorType errorType);
    protected abstract TResult CreateFailureResult(Exception exception);
}

public abstract class CommandHandlerBase<TCommand, TResponse>
    : CommandHandlerAbstract<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull
{
    protected CommandHandlerBase(IUnitOfWork unitOfWork)
        : base(unitOfWork) { }

    protected override Result<TResponse> CreateFailureResult(
        string message,
        ApiErrorType errorType
    ) => Result<TResponse>.Failure(message, errorType);

    protected override Result<TResponse> CreateFailureResult(Exception exception) =>
        Result<TResponse>.Failure(exception);
}

public abstract class CommandHandlerBase<TCommand> : CommandHandlerAbstract<TCommand, Result>
    where TCommand : ICommand
{
    protected CommandHandlerBase(IUnitOfWork unitOfWork)
        : base(unitOfWork) { }

    protected override Result CreateFailureResult(string message, ApiErrorType errorType) =>
        Result.Failure(message, errorType);

    protected override Result CreateFailureResult(Exception exception) => Result.Failure(exception);
}
