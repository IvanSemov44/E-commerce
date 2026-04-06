using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data.Configurations;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Main database context for the E-Commerce application.
/// Uses EF Core with PostgreSQL for data persistence.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher? dispatcher = null) : DbContext(options), IDataProtectionKeyContext
{
    private readonly IDomainEventDispatcher? _dispatcher = dispatcher;

    // Bounded context modules register their EF configuration assemblies here
    // so AppDbContext can apply them without a direct project reference.
    private static readonly HashSet<System.Reflection.Assembly> _additionalConfigurationAssemblies = new();
    private static readonly object _configurationAssemblyLock = new();

    public static void RegisterConfigurationAssembly(System.Reflection.Assembly assembly)
    {
        lock (_configurationAssemblyLock)
        {
            _additionalConfigurationAssemblies.Add(assembly);
        }
    }

    // Users and Authentication
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;

    // Catalog
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductImage> ProductImages { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;

    // Shopping
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Wishlist> Wishlists { get; set; } = null!;

    // Orders and Payments
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode> PromoCodes { get; set; } = null!;

    // Inventory - Legacy from Core
    public DbSet<ECommerce.Core.Entities.InventoryLog> InventoryLogs { get; set; } = null!;

    // Inventory - DDD extract (Phase 3)
    public DbSet<ECommerce.Inventory.Domain.Aggregates.InventoryItem.InventoryItem> InventoryItems { get; set; } = null!;

    // Data Protection Keys for persistent key storage
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    // Integration outbox
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Configurations namespace
        // Each configuration class handles its own entity's mapping and conventions
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);

        // Apply configurations from additional assemblies registered by bounded context modules
        System.Reflection.Assembly[] additionalAssemblies;
        lock (_configurationAssemblyLock)
        {
            additionalAssemblies = _additionalConfigurationAssemblies.ToArray();
        }

        foreach (var assembly in additionalAssemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        // Updated entries: stamp UpdatedAt
        var modifiedEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && e.Entity is Entity)
            .ToList();

        foreach (var entry in modifiedEntries)
        {
            entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = utcNow;
        }

        // Added entries: stamp CreatedAt and UpdatedAt
        var addedEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is Entity)
            .ToList();

        foreach (var entry in addedEntries)
        {
            entry.Property(nameof(Entity.CreatedAt)).CurrentValue = utcNow;
            entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = utcNow;
        }

        // Inventory domain logs are append-only. In EF InMemory + owned collection scenarios,
        // newly appended logs can be tracked as Modified instead of Added, which leads to
        // false DbUpdateConcurrencyException (attempted update of a non-existent row).
        // Normalize these entries to Added before save.
        var modifiedInventoryLogs = ChangeTracker
            .Entries<ECommerce.Inventory.Domain.Aggregates.InventoryItem.InventoryLog>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in modifiedInventoryLogs)
            entry.State = EntityState.Added;

        // Collect and clear domain events before saving to avoid re-entry
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        int result = await base.SaveChangesAsync(cancellationToken);

        if (_dispatcher != null && events.Count != 0)
            await _dispatcher.DispatchEventsAsync(events, cancellationToken);

        return result;
    }
}
