namespace ECommerce.SharedKernel.Interfaces;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; }
    bool IsDeleted => DeletedAt.HasValue;
    void SoftDelete();
}
