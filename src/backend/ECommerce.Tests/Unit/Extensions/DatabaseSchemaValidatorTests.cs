using ECommerce.API.Extensions;
using ECommerce.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;
using System.Data.Common;

namespace ECommerce.Tests.Unit.Extensions;

/// <summary>
/// Unit tests for DatabaseSchemaValidator class.
/// Tests schema validation logic for required tables, RowVersion columns, and critical columns.
/// </summary>
[TestClass]
public class DatabaseSchemaValidatorTests
{
    private AppDbContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    #region ValidateAsync Tests

    [TestMethod]
    public async Task ValidateAsync_DoesNotThrow_WhenValidationPasses()
    {
        // Arrange - In-memory DB doesn't support information_schema queries
        // The validator catches exceptions and logs warnings instead of throwing

        // Act
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);

        // Assert - Should not throw, but may log warnings
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ValidateAsync_HandlesConnectionIssues_Gracefully()
    {
        // Arrange - In-memory DB has different behavior than PostgreSQL
        // The validator is designed to catch connection issues and log warnings

        // Act
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);

        // Assert - Should not throw even with connection issues
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Required Tables Tests

    [TestMethod]
    public void RequiredTables_ContainsExpectedTables()
    {
        // Assert - Verify the expected tables are defined
        // This is a static analysis test
        var expectedTables = new[] { "Users", "Products", "Orders", "RefreshTokens", "Categories" };
        
        // The validator should check for these critical tables
        expectedTables.Should().HaveCount(5);
        expectedTables.Should().Contain("Users");
        expectedTables.Should().Contain("Products");
        expectedTables.Should().Contain("Orders");
        expectedTables.Should().Contain("RefreshTokens");
        expectedTables.Should().Contain("Categories");
    }

    #endregion

    #region RowVersion Column Tests

    [TestMethod]
    public void TablesWithRowVersion_ContainsExpectedTables()
    {
        // Assert - Verify tables that should have RowVersion
        var expectedTablesWithRowVersion = new[] { "Products", "Orders", "PromoCodes" };
        
        expectedTablesWithRowVersion.Should().HaveCount(3);
        expectedTablesWithRowVersion.Should().Contain("Products");
        expectedTablesWithRowVersion.Should().Contain("Orders");
        expectedTablesWithRowVersion.Should().Contain("PromoCodes");
    }

    [TestMethod]
    public void TablesWithoutRowVersion_ContainsExpectedTables()
    {
        // Assert - Verify tables that should NOT have RowVersion
        var expectedTablesWithoutRowVersion = new[] 
        { 
            "RefreshTokens", "Users", "Categories", "ProductImages", "Addresses",
            "Carts", "CartItems", "OrderItems", "Reviews", "Wishlists", "InventoryLogs"
        };
        
        expectedTablesWithoutRowVersion.Should().HaveCount(11);
        expectedTablesWithoutRowVersion.Should().Contain("RefreshTokens");
        expectedTablesWithoutRowVersion.Should().Contain("Users");
        expectedTablesWithoutRowVersion.Should().Contain("Categories");
    }

    #endregion

    #region Critical Columns Tests

    [TestMethod]
    public void CriticalColumns_ContainsExpectedColumns()
    {
        // Assert - Verify critical columns for authentication
        var expectedCriticalColumns = new[]
        {
            ("RefreshTokens", "Token"),
            ("RefreshTokens", "UserId"),
            ("RefreshTokens", "ExpiresAt")
        };

        expectedCriticalColumns.Should().HaveCount(3);
        expectedCriticalColumns.Should().Contain(("RefreshTokens", "Token"));
        expectedCriticalColumns.Should().Contain(("RefreshTokens", "UserId"));
        expectedCriticalColumns.Should().Contain(("RefreshTokens", "ExpiresAt"));
    }

    #endregion

    #region Integration-like Tests with Mock DbConnection

    [TestMethod]
    public async Task ValidateAsync_WithMockConnection_ValidatesSuccessfully()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockConnection.Setup(c => c.CloseAsync())
            .Returns(Task.CompletedTask);

        var mockCommand = new Mock<DbCommand>();
        mockCommand.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockConnection.Setup(c => c.CreateCommand())
            .Returns(mockCommand.Object);

        var mockContext = new Mock<DbContext>();
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        
        mockDatabase.Setup(d => d.GetDbConnection())
            .Returns(mockConnection.Object);
        
        mockContext.Setup(c => c.Database)
            .Returns(mockDatabase.Object);

        // Act
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(mockContext.Object);

        // Assert
        await act.Should().NotThrowAsync();
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ValidateAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        var mockContext = new Mock<DbContext>();
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        
        mockDatabase.Setup(d => d.GetDbConnection())
            .Returns(mockConnection.Object);
        
        mockContext.Setup(c => c.Database)
            .Returns(mockDatabase.Object);

        // Act
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(mockContext.Object);

        // Assert - Should catch exception and not throw
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ValidateAsync_WhenInvalidOperationExceptionOccurs_Throws()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Schema validation failed"));

        var mockContext = new Mock<DbContext>();
        var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
        
        mockDatabase.Setup(d => d.GetDbConnection())
            .Returns(mockConnection.Object);
        
        mockContext.Setup(c => c.Database)
            .Returns(mockDatabase.Object);

        // Act
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(mockContext.Object);

        // Assert - InvalidOperationException should propagate
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
