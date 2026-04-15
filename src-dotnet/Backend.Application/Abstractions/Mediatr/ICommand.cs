using Backend.Domain.Common;
using MediatR;

namespace Backend.Application.Abstractions.Mediatr;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    where TResponse : notnull { }

public interface ICommand : IRequest<Result> { }
