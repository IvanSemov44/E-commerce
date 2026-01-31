using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetByProductIdAsync(Guid productId, bool onlyApproved = true, bool trackChanges = false);
    Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId, bool trackChanges = false);
    Task<Review?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);
    Task<int> GetApprovedReviewCountAsync(Guid productId);
    Task<decimal> GetAverageRatingAsync(Guid productId);
    Task<bool> UserHasReviewedAsync(Guid userId, Guid productId);
    Task<IEnumerable<Review>> GetPendingApprovalAsync(bool trackChanges = false);
}
