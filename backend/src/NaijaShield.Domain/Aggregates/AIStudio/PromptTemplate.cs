using NaijaShield.Domain.Common;

namespace NaijaShield.Domain.Aggregates.AIStudio;

/// <summary>A versioned, tenant-overridable prompt template used by Semantic Kernel.</summary>
public class PromptTemplate : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Language { get; private set; } = "en";
    public string UseCase { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public int Version { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;
    public int TimesUsed { get; private set; }
    public decimal SuccessRate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid TenantId { get; private set; }

    private PromptTemplate() { }

    public static PromptTemplate Create(
        Guid tenantId,
        Guid createdByUserId,
        string name,
        string language,
        string useCase,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new PromptTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedByUserId = createdByUserId,
            Name = name,
            Language = language,
            UseCase = useCase,
            Content = content,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public PromptTemplate NewVersion(string newContent, Guid updatedBy)
    {
        return new PromptTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            CreatedByUserId = updatedBy,
            Name = Name,
            Language = Language,
            UseCase = UseCase,
            Content = newContent,
            Version = Version + 1,
            IsActive = false, // activate explicitly
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public void RecordUsage(bool success)
    {
        TimesUsed++;
        if (TimesUsed > 0)
        {
            SuccessRate = ((SuccessRate * (TimesUsed - 1)) + (success ? 1 : 0)) / TimesUsed;
        }
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>A Semantic Kernel plugin function registered in the AI pipeline.</summary>
public class SemanticKernelSkill : AggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FunctionType { get; set; } = string.Empty; // Native | Semantic
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

/// <summary>An Azure OpenAI model deployment tracked by NaijaShield.</summary>
public class ModelDeployment : AggregateRoot<Guid>
{
    public string Language { get; set; } = "en";
    public string AzureDeploymentName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastFineTunedAt { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}
