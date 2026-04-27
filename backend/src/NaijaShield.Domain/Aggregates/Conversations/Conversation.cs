using NaijaShield.Domain.Common;
using NaijaShield.Domain.Enums;

namespace NaijaShield.Domain.Aggregates.Conversations;

/// <summary>An ongoing or historical multi-message conversation with a subscriber.</summary>
public class Conversation : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid TenantId { get; private set; }
    public string CustomerMsisdn { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public DateTime LastMessageAt { get; private set; }
    public ConversationStatus Status { get; private set; }
    public Channel Channel { get; private set; }

    /// <summary>Alias for Channel — used by legacy code.</summary>
    public string CurrentChannel => Channel.ToString();

    public string Language { get; private set; } = "en";

    /// <summary>Alias for Language — used by legacy code.</summary>
    public string DetectedLanguage => Language;

    public decimal SentimentScore { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public bool IsScamFlagged { get; private set; }
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Alias for Summary — used by legacy code.</summary>
    public string AiSummary => Summary;

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private Conversation() { }

    public static Conversation Open(Guid tenantId, Guid customerId, string channel, string detectedLanguage = "en", string customerMsisdn = "")
    {
        Enum.TryParse<Channel>(channel, ignoreCase: true, out var channelEnum);
        return new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            CustomerMsisdn = customerMsisdn,
            Channel = channelEnum,
            Language = detectedLanguage,
            Status = ConversationStatus.Open,
            StartedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddMessage(Message message)
    {
        _messages.Add(message);
        LastMessageAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Add a message and return it (Application layer helper).</summary>
    public Message AddMessage(string content, string contentEnglish, Guid agentId, bool isFromCustomer)
    {
        var msg = Message.Create(Id, content, contentEnglish, isFromCustomer ? "Inbound" : "Outbound");
        _messages.Add(msg);
        AssignedAgentId ??= agentId;
        LastMessageAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return msg;
    }

    public void Close(Guid agentId, string summary)
    {
        Status = ConversationStatus.Closed;
        EndedAt = DateTime.UtcNow;
        Summary = summary;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Assign(Guid agentId)
    {
        AssignedAgentId = agentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FlagAsScam()
    {
        IsScamFlagged = true;
        Status = ConversationStatus.ScamFlagged;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Escalate()
    {
        Status = ConversationStatus.Escalated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = ConversationStatus.Closed;
        EndedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSummary(string summary)
    {
        Summary = summary;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSentiment(decimal score)
    {
        SentimentScore = score;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>A single message within a conversation.</summary>
public class Message : Entity<Guid>
{
    public Guid ConversationId { get; set; }
    public Channel Channel { get; set; }
    public string Direction { get; set; } = "Inbound"; // Inbound | Outbound | System
    public string ContentOriginal { get; set; } = string.Empty;

    /// <summary>Alias for ContentOriginal — used by Application layer.</summary>
    public string Content => ContentOriginal;

    public string Language { get; set; } = "en";
    public string ContentEnglish { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public MessageType Type { get; set; }

    /// <summary>Alias for Type — used by Application layer.</summary>
    public MessageType MessageType => Type;

    /// <summary>True if this message originated from the customer.</summary>
    public bool IsFromCustomer => Direction == "Inbound";

    public string? AudioBlobUri { get; set; }
    public string? AiSuggestedReply { get; set; }

    internal static Message Create(Guid conversationId, string content, string contentEnglish, string direction) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        ContentOriginal = content,
        ContentEnglish = contentEnglish,
        Direction = direction,
        SentAt = DateTime.UtcNow,
        Type = MessageType.Text
    };
}
