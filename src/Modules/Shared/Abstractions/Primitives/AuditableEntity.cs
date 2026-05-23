using LabViroMol.Modules.Shared.Abstractions.Identity;

namespace LabViroMol.Modules.Shared.Abstractions.Primitives;

public abstract class AuditableEntity<TId> : BaseEntity<TId>
{
    protected AuditableEntity() { }

    protected AuditableEntity(TId id, UserId createdBy) : base(id)
    { 
        if (id is null) throw new ArgumentNullException(nameof(id));

        Id = id; 
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        IsDeleted = false;
    }

    protected AuditableEntity(TId id) : base(id)
    {
        if (id is null) throw new ArgumentNullException(nameof(id));

        Id = id; 
        CreatedAt = DateTimeOffset.UtcNow;
        IsDeleted = false;
    }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? RemovedBy { get; protected set; }
    public DateTimeOffset? RemovedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }

    public void MarkAsUpdated(UserId modifiedBy)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = modifiedBy;
    }

    public void MarkAsRemoved(UserId removedBy)
    {
        if (IsDeleted) return;

        RemovedAt = DateTimeOffset.UtcNow;
        RemovedBy = removedBy;
        IsDeleted = true;
    }
}