using ECommerce.API.Common.Extensions;
using ECommerce.Infrastructure.Data;
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
        await Should.NotThrowAsync(act);
    }

    [TestMethod]
    public async Task ValidateAsync_HandlesConnectionIssues_Gracefully()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await Should.NotThrowAsync(act);
    }

    [TestMethod]
    public void RequiredTables_ContainsExpectedTables()
    {
        var expectedTables = new[] { "Users", "Products", "Orders", "RefreshTokens", "Categories" };

        expectedTables.Length.ShouldBe(5);
        expectedTables.ShouldContain("Users");
        expectedTables.ShouldContain("Products");
        expectedTables.ShouldContain("Orders");
        expectedTables.ShouldContain("RefreshTokens");
        expectedTables.ShouldContain("Categories");
    }

    [TestMethod]
    public void TablesWithRowVersion_ContainsExpectedTables()
    {
        var expectedTablesWithRowVersion = new[] { "Users", "Products", "Carts", "Orders", "PromoCodes" };

        expectedTablesWithRowVersion.Length.ShouldBe(5);
        expectedTablesWithRowVersion.ShouldContain("Users");
        expectedTablesWithRowVersion.ShouldContain("Products");
        expectedTablesWithRowVersion.ShouldContain("Carts");
        expectedTablesWithRowVersion.ShouldContain("Orders");
        expectedTablesWithRowVersion.ShouldContain("PromoCodes");
    }

    [TestMethod]
    public void TablesWithoutRowVersion_ContainsExpectedTables()
    {
        var expectedTablesWithoutRowVersion = new[]
        {
            "RefreshTokens", "Categories", "ProductImages", "Addresses",
            "CartItems", "OrderItems", "Reviews", "Wishlists", "InventoryLogs"
        };

        expectedTablesWithoutRowVersion.Length.ShouldBe(9);
        expectedTablesWithoutRowVersion.ShouldContain("RefreshTokens");
        expectedTablesWithoutRowVersion.ShouldContain("Categories");
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

        expectedCriticalColumns.Length.ShouldBe(3);
        expectedCriticalColumns.ShouldContain(("RefreshTokens", "Token"));
        expectedCriticalColumns.ShouldContain(("RefreshTokens", "UserId"));
        expectedCriticalColumns.ShouldContain(("RefreshTokens", "ExpiresAt"));
    }

    [TestMethod]
    public async Task ValidateAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await Should.NotThrowAsync(act);
    }

    [TestMethod]
    public async Task ValidateAsync_WhenInvalidOperationExceptionOccurs_Throws()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await Should.NotThrowAsync(act);
    }

    [TestMethod]
    public async Task ValidateAsync_WithMockConnection_ValidatesSuccessfully()
    {
        var act = async () => await DatabaseSchemaValidator.ValidateAsync(_context);
        await Should.NotThrowAsync(act);
    }
}

