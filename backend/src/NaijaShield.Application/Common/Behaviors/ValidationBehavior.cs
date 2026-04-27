using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using NaijaShield.Application.Common;

namespace NaijaShield.Application.Common.Behaviors;

/// <summary>
/// Executes FluentValidation validators before the handler runs.
/// Aggregates all failures and returns a <see cref="Result{T}"/> failure instead of throwing.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));
        logger.LogWarning("Validation failed for {RequestType}: {Errors}", typeof(TRequest).Name, errors);

        // Return Result.Failure if TResponse is a Result type; otherwise throw.
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(nameof(Result.Failure), [typeof(string)])!;
            return (TResponse)failureMethod.Invoke(null, [errors])!;
        }

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        throw new ValidationException(failures);
    }
}
