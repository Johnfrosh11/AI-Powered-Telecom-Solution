using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Behaviors;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Application.Features.Conversations;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ConversationDto(
    Guid Id, Guid CustomerId, string CustomerMsisdn, string Channel,
    string Status, string Language, string? AssignedAgentId, string? Summary,
    DateTime CreatedAt, DateTime LastMessageAt, int MessageCount, Guid TenantId);

public record MessageDto(
    Guid Id, string Content, string ContentEn, string MessageType,
    bool IsFromCustomer, DateTime SentAt, bool IsTranslated);

// ── Reply Command ─────────────────────────────────────────────────────────────

public record ReplyConversationCommand(
    Guid ConversationId, Guid TenantId, Guid AgentUserId, string Content)
    : IRequest<Result<Guid>>, ITransactionalCommand;

public class ReplyConversationCommandHandler(
    IConversationRepository conversations,
    IScamDetectionAiService aiService)
    : IRequestHandler<ReplyConversationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ReplyConversationCommand cmd, CancellationToken ct)
    {
        var convo = await conversations.GetByIdAsync(cmd.ConversationId, ct);
        if (convo is null || convo.TenantId != cmd.TenantId)
            return Result.Failure<Guid>("Conversation not found.");

        if (convo.Status == ConversationStatus.Closed)
            return Result.Failure<Guid>("Cannot reply to a closed conversation.");

        var contentEn = convo.Language == "en"
            ? cmd.Content
            : await aiService.TranslateAsync(cmd.Content, convo.Language, "en", ct);

        var message = convo.AddMessage(cmd.Content, contentEn, cmd.AgentUserId, isFromCustomer: false);
        return Result.Success(message.Id);
    }
}

// ── Close Conversation ────────────────────────────────────────────────────────

public record CloseConversationCommand(Guid ConversationId, Guid TenantId, Guid AgentUserId)
    : IRequest<Result>, ITransactionalCommand;

public class CloseConversationCommandHandler(
    IConversationRepository conversations,
    IScamDetectionAiService aiService)
    : IRequestHandler<CloseConversationCommand, Result>
{
    public async Task<Result> Handle(CloseConversationCommand cmd, CancellationToken ct)
    {
        var convo = await conversations.GetByIdAsync(cmd.ConversationId, ct);
        if (convo is null || convo.TenantId != cmd.TenantId)
            return Result.Failure("Conversation not found.");

        var summary = await aiService.SummarizeConversationAsync(
            string.Join(" ", convo.Messages.Select(m => m.ContentEnglish)), ct);

        convo.Close(cmd.AgentUserId, summary);
        return Result.Success();
    }
}

// ── List Conversations Query ───────────────────────────────────────────────────

public record ListConversationsQuery(
    Guid TenantId, string? Status, string? Channel, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<ConversationDto>>>;

public class ListConversationsQueryHandler(IConversationRepository conversations)
    : IRequestHandler<ListConversationsQuery, Result<PagedResult<ConversationDto>>>
{
    public async Task<Result<PagedResult<ConversationDto>>> Handle(ListConversationsQuery q, CancellationToken ct)
    {
        var items = await conversations.ListAsync(q.TenantId, q.Channel, null, q.Status, q.Page, q.PageSize, ct);
        var total = await conversations.CountAsync(q.TenantId, q.Channel, null, q.Status, ct);

        var dtos = items.Select(c => new ConversationDto(
            c.Id, c.CustomerId, string.Empty, c.CurrentChannel, c.Status.ToString(),
            c.Language, c.AssignedAgentId?.ToString(), c.AiSummary,
            c.CreatedAt, c.StartedAt, c.Messages.Count, c.TenantId)).ToList();

        return Result.Success(new PagedResult<ConversationDto>(dtos, total, q.Page, q.PageSize));
    }
}

// ── Get Conversation Messages ──────────────────────────────────────────────────

public record GetConversationMessagesQuery(Guid ConversationId, Guid TenantId)
    : IRequest<Result<IReadOnlyList<MessageDto>>>;

public class GetConversationMessagesQueryHandler(IConversationRepository conversations)
    : IRequestHandler<GetConversationMessagesQuery, Result<IReadOnlyList<MessageDto>>>
{
    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(GetConversationMessagesQuery q, CancellationToken ct)
    {
        var convo = await conversations.GetByIdAsync(q.ConversationId, ct);
        if (convo is null || convo.TenantId != q.TenantId)
            return Result.Failure<IReadOnlyList<MessageDto>>("Conversation not found.");

        var dtos = convo.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto(
                m.Id, m.Content, m.ContentEnglish, m.MessageType.ToString(),
                m.IsFromCustomer, m.SentAt, m.Content != m.ContentEnglish))
            .ToList();

        return Result.Success<IReadOnlyList<MessageDto>>(dtos);
    }
}
