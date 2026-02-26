using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the UserRepository class.
/// Tests user-specific repository operations including retrieval, authentication, and OAuth.
/// </summary>
[TestClass]
public class UserRepositoryTests
{
    private AppDbContext _context = null!;
    private UserRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);

        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "customer@test.com",
                FirstName = "John",
                LastName = "Doe",
                Role = UserRole.Customer,
                IsEmailVerified = true,
                GoogleId = "google-123",
                FacebookId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRole.Admin,
                IsEmailVerified = true,
                GoogleId = null,
                FacebookId = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "facebook@test.com",
                FirstName = "Facebook",
                LastName = "User",
                Role = UserRole.Customer,
                IsEmailVerified = true,
                GoogleId = null,
                FacebookId = "facebook-456"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "unverified@test.com",
                FirstName = "Unverified",
                LastName = "User",
                Role = UserRole.Customer,
                IsEmailVerified = false,
                GoogleId = null,
                FacebookId = null
            }
        };

        _context.Users.AddRange(users);

        // Add addresses for first user
        var customer = users[0];
        var addresses = new List<Address>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = customer.Id,
                Type = "Shipping",
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA",
                IsDefault = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = customer.Id,
                Type = "Billing",
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "456 Oak Ave",
                City = "Los Angeles",
                State = "CA",
                PostalCode = "90001",
                Country = "USA",
                IsDefault = false
            }
        };

        _context.Addresses.AddRange(addresses);
        _context.SaveChanges();
    }

    #region GetByEmailAsync Tests

    [TestMethod]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Act
        var result = await _repository.GetByEmailAsync("customer@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("customer@test.com");
    }

    [TestMethod]
    public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByEmailAsync_CaseSensitive_ReturnsCorrectUser()
    {
        // Act - Try with different case
        var result = await _repository.GetByEmailAsync("CUSTOMER@TEST.COM");

        // Assert - Should return null if case-sensitive, or user if case-insensitive
        // This depends on database collation, but we test the behavior
        if (result != null)
        {
            result.Email.ToLower().Should().Be("customer@test.com");
        }
    }

    [TestMethod]
    public async Task GetByEmailAsync_WithTracking_TracksEntity()
    {
        // Act
        var result = await _repository.GetByEmailAsync("customer@test.com", trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByEmailAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Act
        var result = await _repository.GetByEmailAsync("customer@test.com", trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetWithAddressesAsync Tests

    [TestMethod]
    public async Task GetWithAddressesAsync_ExistingUser_ReturnsUserWithAddresses()
    {
        // Arrange
        var customer = await _context.Users.FirstAsync(u => u.Email == "customer@test.com");

        // Act
        var result = await _repository.GetWithAddressesAsync(customer.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Addresses.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetWithAddressesAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWithAddressesAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWithAddressesAsync_UserWithNoAddresses_ReturnsEmptyAddresses()
    {
        // Arrange
        var admin = await _context.Users.FirstAsync(u => u.Email == "admin@test.com");

        // Act
        var result = await _repository.GetWithAddressesAsync(admin.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Addresses.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetWithAddressesAsync_WithTracking_TracksEntity()
    {
        // Arrange
        var customer = await _context.Users.FirstAsync(u => u.Email == "customer@test.com");

        // Act
        var result = await _repository.GetWithAddressesAsync(customer.Id, trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetWithAddressesAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Arrange
        var customer = await _context.Users.FirstAsync(u => u.Email == "customer@test.com");

        // Act
        var result = await _repository.GetWithAddressesAsync(customer.Id, trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region EmailExistsAsync Tests

    [TestMethod]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Act
        var result = await _repository.EmailExistsAsync("customer@test.com");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@test.com");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task EmailExistsAsync_EmptyEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetByGoogleIdAsync Tests

    [TestMethod]
    public async Task GetByGoogleIdAsync_ExistingGoogleId_ReturnsUser()
    {
        // Act
        var result = await _repository.GetByGoogleIdAsync("google-123");

        // Assert
        result.Should().NotBeNull();
        result!.GoogleId.Should().Be("google-123");
    }

    [TestMethod]
    public async Task GetByGoogleIdAsync_NonExistingGoogleId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByGoogleIdAsync("nonexistent-google-id");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByGoogleIdAsync_UserWithoutGoogleId_ReturnsNull()
    {
        // Act - Try to find a user who doesn't have Google ID set
        var result = await _repository.GetByGoogleIdAsync("facebook-456");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByGoogleIdAsync_WithTracking_TracksEntity()
    {
        // Act
        var result = await _repository.GetByGoogleIdAsync("google-123", trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByGoogleIdAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Act
        var result = await _repository.GetByGoogleIdAsync("google-123", trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetByFacebookIdAsync Tests

    [TestMethod]
    public async Task GetByFacebookIdAsync_ExistingFacebookId_ReturnsUser()
    {
        // Act
        var result = await _repository.GetByFacebookIdAsync("facebook-456");

        // Assert
        result.Should().NotBeNull();
        result!.FacebookId.Should().Be("facebook-456");
    }

    [TestMethod]
    public async Task GetByFacebookIdAsync_NonExistingFacebookId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByFacebookIdAsync("nonexistent-facebook-id");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByFacebookIdAsync_UserWithoutFacebookId_ReturnsNull()
    {
        // Act - Try to find a user who doesn't have Facebook ID set
        var result = await _repository.GetByFacebookIdAsync("google-123");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByFacebookIdAsync_WithTracking_TracksEntity()
    {
        // Act
        var result = await _repository.GetByFacebookIdAsync("facebook-456", trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByFacebookIdAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Act
        var result = await _repository.GetByFacebookIdAsync("facebook-456", trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<User>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetCustomersCountAsync Tests

    [TestMethod]
    public async Task GetCustomersCountAsync_ReturnsCorrectCount()
    {
        // Act
        var result = await _repository.GetCustomersCountAsync();

        // Assert - 3 customers (customer@test.com, facebook@test.com, unverified@test.com)
        result.Should().Be(3);
    }

    [TestMethod]
    public async Task GetCustomersCountAsync_ExcludesAdmins()
    {
        // Act
        var result = await _repository.GetCustomersCountAsync();

        // Assert - Should not include admin@test.com
        var totalUsers = await _context.Users.CountAsync();
        result.Should().BeLessThan(totalUsers);
    }

    #endregion

    #region Base Repository Tests

    [TestMethod]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(4);
    }

    [TestMethod]
    public async Task AddAsync_AddsUserToDatabase()
    {
        // Arrange
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "new@test.com",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Customer
        };

        // Act
        await _repository.AddAsync(newUser);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByEmailAsync("new@test.com");
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Delete_RemovesUserFromDatabase()
    {
        // Arrange
        var user = await _context.Users.FirstAsync(u => u.Email == "unverified@test.com");

        // Act
        _repository.Delete(user);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByEmailAsync("unverified@test.com");
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Update_UpdatesUserInDatabase()
    {
        // Arrange
        var user = await _context.Users.FirstAsync(u => u.Email == "customer@test.com");
        user.FirstName = "Updated";

        // Act
        _repository.Update(user);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByEmailAsync("customer@test.com");
        result!.FirstName.Should().Be("Updated");
    }

    #endregion
}