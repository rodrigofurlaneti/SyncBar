using MediatR;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
