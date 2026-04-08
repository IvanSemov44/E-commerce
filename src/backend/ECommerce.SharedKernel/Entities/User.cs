using System.ComponentModel.DataAnnotations;
using ECommerce.SharedKernel.Common;
using ECommerce.SharedKernel.Enums;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.SharedKernel.Entities;

public class User : BaseEntity, IConcurrencyToken
{
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpires { get; set; }
    public string? GoogleId { get; set; }
    public string? FacebookId { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>Concurrency token for optimistic locking.</summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual Cart? Cart { get; set; }
}
