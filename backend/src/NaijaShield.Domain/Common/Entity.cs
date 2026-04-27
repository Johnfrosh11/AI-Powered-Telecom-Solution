namespace NaijaShield.Domain.Common;

/// <summary>Non-generic marker base used for EF Core type constraints.</summary>
public abstract class Entity { }

/// <summary>Base entity with strongly-typed identifier and audit stamps.</summary>
public abstract class Entity<TId> : Entity
{
    /// <summary>Primary key.</summary>
    public TId Id { get; set; } = default!;

    /// <summary>UTC timestamp of creation.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of last modification.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool IsDeleted { get; set; }
}
