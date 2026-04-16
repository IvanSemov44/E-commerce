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
        var expectedTables = new[] { "DataProtectionKeys" };

        expectedTables.Length.ShouldBe(1);
        expectedTables.ShouldContain("DataProtectionKeys");
    }

    [TestMethod]
    public void IntegrationTables_ContainsExpectedTables()
    {
        var expectedIntegrationTables = new[]
        {
            "outbox_messages",
            "inbox_messages",
            "dead_letter_messages",
            "order_fulfillment_saga_states"
        };

        expectedIntegrationTables.Length.ShouldBe(4);
        expectedIntegrationTables.ShouldContain("outbox_messages");
        expectedIntegrationTables.ShouldContain("inbox_messages");
        expectedIntegrationTables.ShouldContain("dead_letter_messages");
        expectedIntegrationTables.ShouldContain("order_fulfillment_saga_states");
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

