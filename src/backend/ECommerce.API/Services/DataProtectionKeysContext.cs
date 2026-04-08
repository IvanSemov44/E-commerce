using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

/// <summary>
/// Dedicated context for Data Protection key persistence.
/// Keeping this separate avoids coupling key storage to AppDbContext composition.
/// </summary>
public sealed class DataProtectionKeysContext(DbContextOptions<DataProtectionKeysContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}
