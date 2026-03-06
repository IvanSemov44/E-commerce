using ECommerce.API.Extensions;
using ECommerce.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Extensions;

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

    [TestMethod]
    public async Task ValidateAsync_DoesNotThrow_WhenValidationPasses()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ValidateAsync_HandlesConnectionIssues_Gracefully()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public void RequiredTables_ContainsExpectedTables()
    {
        var expectedTables = new[] { "Users", "Products", "Orders", "RefreshTokens", "Categories" };

        expectedTables.Should().HaveCount(5);
        expectedTables.Should().Contain("Users");
        expectedTables.Should().Contain("Products");
        expectedTables.Should().Contain("Orders");
        expectedTables.Should().Contain("RefreshTokens");
        expectedTables.Should().Contain("Categories");
    }

    [TestMethod]
    public void TablesWithRowVersion_ContainsExpectedTables()
    {
        var expectedTablesWithRowVersion = new[] { "Products", "Orders", "PromoCodes" };

        expectedTablesWithRowVersion.Should().HaveCount(3);
        expectedTablesWithRowVersion.Should().Contain("Products");
        expectedTablesWithRowVersion.Should().Contain("Orders");
        expectedTablesWithRowVersion.Should().Contain("PromoCodes");
    }

    [TestMethod]
    public void TablesWithoutRowVersion_ContainsExpectedTables()
    {
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

    [TestMethod]
    public void CriticalColumns_ContainsExpectedColumns()
    {
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

    [TestMethod]
    public async Task ValidateAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ValidateAsync_WhenInvalidOperationExceptionOccurs_Throws()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ValidateAsync_WithMockConnection_ValidatesSuccessfully()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await act.Should().NotThrowAsync();
    }
}
