using MediatR;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Common.Behaviors;

/// <summary>
/// Automatically writes an audit log entry for commands decorated with <see cref="IAuditableCommand"/>.
/// </summary>
public class AuditBehavior<TRequest, TResponse>(
    IAuditLogger auditLogger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuditableCommand auditCmd)
        {
            return await next();
        }

        TResponse? response = default;
        bool success = true;

        try
        {
            response = await next();
            return response;
        }
        catch
        {
            success = false;
            throw;
        }
        finally
        {
            await auditLogger.LogAsync(
                tenantId: currentUser.TenantId ?? Guid.Empty,
                userId: currentUser.UserId,
                actorType: "User",
                action: auditCmd.AuditAction,
                targetType: auditCmd.AuditTargetType,
                targetId: auditCmd.AuditTargetId ?? string.Empty,
                success: success,
                sensitivity: auditCmd.AuditSensitivity,
                ct: cancellationToken);
        }
    }
}

/// <summary>Implement on commands that must produce an audit trail.</summary>
public interface IAuditableCommand
{
    string AuditAction { get; }
    string AuditTargetType { get; }
    string? AuditTargetId { get; }
    string AuditSensitivity => "Medium";
}
