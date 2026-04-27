using MediatR;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Common.Behaviors;

/// <summary>
/// Wraps every command in a DB transaction; commits on success, rolls back on exception.
/// Queries are excluded by convention (must implement <see cref="ITransactionalCommand"/>).
/// </summary>
public class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only wrap commands; queries skip the UoW save.
        if (request is not ITransactionalCommand)
        {
            return await next();
        }

        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}

/// <summary>Marker interface — implement on commands that require DB persistence.</summary>
public interface ITransactionalCommand { }
