using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ECommerce.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds user data into the database.
/// </summary>
public class UserSeeder : IUserSeeder
{
    public async Task SeedAsync(AppDbContext context)
    {
        try
        {
            // Check if users already exist
            if (await context.Users.AnyAsync())
            {
                return; // Database already seeded
            }

            var users = new List<User>
            {
                // Admin user
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Phone = "+1-555-0001",
                    Role = UserRole.Admin,
                    IsEmailVerified = true,
                    PasswordHash = HashPassword("Admin123"),
                    CreatedAt = DateTime.UtcNow
                },
                // Customer users
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Phone = "+1-555-0100",
                    Role = UserRole.Customer,
                    IsEmailVerified = true,
                    PasswordHash = HashPassword("Customer123"),
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Phone = "+1-555-0101",
                    Role = UserRole.Customer,
                    IsEmailVerified = true,
                    PasswordHash = HashPassword("Customer123"),
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "michael.johnson@example.com",
                    FirstName = "Michael",
                    LastName = "Johnson",
                    Phone = "+1-555-0102",
                    Role = UserRole.Customer,
                    IsEmailVerified = true,
                    PasswordHash = HashPassword("Customer123"),
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "sarah.williams@example.com",
                    FirstName = "Sarah",
                    LastName = "Williams",
                    Phone = "+1-555-0103",
                    Role = UserRole.Customer,
                    IsEmailVerified = true,
                    PasswordHash = HashPassword("Customer123"),
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding users: {ex.Message}");
            throw;
        }
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
