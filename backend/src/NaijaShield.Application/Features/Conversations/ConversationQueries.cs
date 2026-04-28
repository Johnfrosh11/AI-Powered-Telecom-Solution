using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.Conversations;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ConversationListItemDto(
    Guid Id, Guid CustomerId, string CustomerMsisdn, string Status,
    string Channel, string Language, Guid? AssignedAgentId,
    int MessageCount, DateTime LastMessageAt, Guid TenantId);

public record ConversationDetailDto(
    Guid Id, Guid CustomerId, string CustomerMsisdn, string Status,
    string Channel, string Language, Guid? AssignedAgentId,
    int MessageCount, DateTime LastMessageAt, DateTime CreatedAt,
    DateTime? EndedAt, Guid TenantId);

// ── Get Conversation By ID Query ──────────────────────────────────────────────

public record GetConversationByIdQuery(Guid ConversationId, Guid TenantId)
    : IRequest<Result<ConversationDetailDto>>;

public class GetConversationByIdQueryHandler(IConversationRepository conversations)
    : IRequestHandler<GetConversationByIdQuery, Result<ConversationDetailDto>>
{
    public async Task<Result<ConversationDetailDto>> Handle(GetConversationByIdQuery q, CancellationToken ct)
    {
        var c = await conversations.GetByIdAsync(q.ConversationId, ct);
        if (c is null || c.TenantId != q.TenantId)
            return Result.Failure<ConversationDetailDto>("Conversation not found.");

        return Result.Success(new ConversationDetailDto(
            c.Id, c.CustomerId, c.CustomerMsisdn, c.Status.ToString(),
            c.Channel.ToString(), c.Language, c.AssignedAgentId,
            c.Messages.Count, c.LastMessageAt, c.CreatedAt, c.EndedAt, c.TenantId));
    }
}

// ── Escalate Conversation Command ─────────────────────────────────────────────

public record EscalateConversationCommand(Guid ConversationId, Guid TenantId, string Reason)
    : IRequest<Result>, ITransactionalCommand, IAuditableCommand
{
    public string AuditAction => "conversation.escalated";
    public string AuditTargetType => "Conversation";
    public string? AuditTargetId => ConversationId.ToString();
}

public class EscalateConversationCommandHandler(IConversationRepository conversations)
    : IRequestHandler<EscalateConversationCommand, Result>
{
    public async Task<Result> Handle(EscalateConversationCommand cmd, CancellationToken ct)
    {
        var c = await conversations.GetByIdAsync(cmd.ConversationId, ct);
        if (c is null || c.TenantId != cmd.TenantId)
            return Result.Failure("Conversation not found.");

        c.Escalate();
        return Result.Success();
    }
}
