using ECommerce.Promotions.Application.DTOs.Common;
using ECommerce.Promotions.Application.Queries.GetPromoCode;
using ECommerce.Promotions.Application.Queries.GetPromoCodes;
using ECommerce.Promotions.Application.Queries.ValidatePromoCode;
using ECommerce.Promotions.Domain.Errors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Handlers;

[TestClass]
public class QueryHandlerTests
{
    [TestMethod]
    public async Task GetPromoCodes_ReturnsPaginatedResults()
    {
        var repo = new FakePromoCodeRepository();
        repo.Seed(PromoCodeFactory.Active("SAVE20", createdAt: DateTime.UtcNow.AddMinutes(-10)));
        repo.Seed(PromoCodeFactory.Active("WINTER10", createdAt: DateTime.UtcNow.AddMinutes(-5)));
        var handler = new GetPromoCodesQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodesQuery(1, 10), default);

        Assert.IsTrue(result.IsSuccess);
        var page = result.GetDataOrThrow();
        Assert.IsInstanceOfType<PaginatedList<Application.DTOs.PromoCodeListItemDto>>(page);
        Assert.AreEqual(2, page.TotalCount);
        Assert.HasCount(2, page.Items);
        Assert.AreEqual("WINTER10", page.Items[0].Code);
    }

    [TestMethod]
    public async Task GetPromoCodes_ClampsPageSizeTo100()
    {
        var repo = new FakePromoCodeRepository();
        for (var i = 0; i < 150; i++)
        {
            repo.Seed(PromoCodeFactory.Active($"CODE{i:000}", createdAt: DateTime.UtcNow.AddMinutes(-i)));
        }

        var handler = new GetPromoCodesQueryHandler(repo);
        var result = await handler.Handle(new GetPromoCodesQuery(1, 500), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.GetDataOrThrow().PageSize);
        Assert.AreEqual(150, result.GetDataOrThrow().TotalCount);
        Assert.HasCount(100, result.GetDataOrThrow().Items);
    }

    [TestMethod]
    public async Task GetPromoCodes_FiltersBySearchAndActiveFlag()
    {
        var repo = new FakePromoCodeRepository();
        var active = PromoCodeFactory.Active("SUMMER20", createdAt: DateTime.UtcNow.AddMinutes(-5));
        var inactive = PromoCodeFactory.Active("WINTER10", createdAt: DateTime.UtcNow.AddMinutes(-10));
        inactive.Deactivate();
        repo.Seed(active);
        repo.Seed(inactive);

        var handler = new GetPromoCodesQueryHandler(repo);
        var result = await handler.Handle(new GetPromoCodesQuery(1, 10, Search: "SUM", IsActive: true), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.GetDataOrThrow().TotalCount);
        Assert.AreEqual("SUMMER20", result.GetDataOrThrow().Items.Single().Code);
    }

    [TestMethod]
    public async Task GetPromoCode_ReturnsDtoForExistingItem()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new GetPromoCodeQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodeQuery(promo.Id), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SAVE20", result.GetDataOrThrow().Code);
        Assert.AreEqual(promo.Id, result.GetDataOrThrow().Id);
    }

    [TestMethod]
    public async Task GetPromoCode_UnknownId_ReturnsPromoNotFound()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new GetPromoCodeQueryHandler(repo);

        var result = await handler.Handle(new GetPromoCodeQuery(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.PromoNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task ValidatePromoCode_ValidActivePromo_ReturnsSuccess()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20", 20, createdAt: DateTime.UtcNow.AddDays(-1));
        repo.Seed(promo);
        var handler = new ValidatePromoCodeQueryHandler(repo);

        var result = await handler.Handle(new ValidatePromoCodeQuery("save20", 100), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.GetDataOrThrow().IsValid);
        Assert.AreEqual(20m, result.GetDataOrThrow().DiscountAmount);
        Assert.AreEqual("SAVE20", result.GetDataOrThrow().Code);
    }

    [TestMethod]
    public async Task ValidatePromoCode_InvalidFormat_ReturnsFalse()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new ValidatePromoCodeQueryHandler(repo);

        var result = await handler.Handle(new ValidatePromoCodeQuery("  ", 100), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.GetDataOrThrow().IsValid);
        Assert.AreEqual("Invalid promo code format", result.GetDataOrThrow().Message);
    }

    [TestMethod]
    public async Task ValidatePromoCode_UnknownCode_ReturnsNotFoundMessage()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new ValidatePromoCodeQueryHandler(repo);

        var result = await handler.Handle(new ValidatePromoCodeQuery("MISSING", 100), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.GetDataOrThrow().IsValid);
        Assert.AreEqual("Promo code not found", result.GetDataOrThrow().Message);
    }

    [TestMethod]
    public async Task ValidatePromoCode_ExpiredPromo_ReturnsNotValid()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.ActiveWithPeriod(
            code: "SAVE20",
            percent: 20,
            validFrom: DateTime.UtcNow.AddDays(-10),
            validUntil: DateTime.UtcNow.AddDays(-1));
        repo.Seed(promo);
        var handler = new ValidatePromoCodeQueryHandler(repo);

        var result = await handler.Handle(new ValidatePromoCodeQuery("SAVE20", 100), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.GetDataOrThrow().IsValid);
        Assert.AreEqual(0m, result.GetDataOrThrow().DiscountAmount);
        Assert.AreEqual(PromotionsErrors.PromoNotValid.Message, result.GetDataOrThrow().Message);
    }
}
