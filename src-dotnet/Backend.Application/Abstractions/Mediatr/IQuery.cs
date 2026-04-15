using Backend.Domain.Common;
using MediatR;

namespace Backend.Application.Abstractions.Mediatr;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    where TResponse : notnull { }

public interface IQuery : IRequest<Result> { }
