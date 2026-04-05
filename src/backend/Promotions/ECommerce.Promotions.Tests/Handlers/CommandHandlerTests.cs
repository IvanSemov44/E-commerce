using ECommerce.Promotions.Application.Commands.CreatePromoCode;
using ECommerce.Promotions.Application.Commands.DeactivatePromoCode;
using ECommerce.Promotions.Application.Commands.UpdatePromoCode;
using ECommerce.Promotions.Domain.Errors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Promotions.Tests.Handlers;

[TestClass]
public class CommandHandlerTests
{
    [TestMethod]
    public async Task CreatePromoCode_ValidData_ReturnsDetailDto()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new CreatePromoCodeCommandHandler(repo);

        var command = new CreatePromoCodeCommand(
            Code: "SUMMER10",
            DiscountType: "Percentage",
            DiscountValue: 10,
            MinimumOrderAmount: 100,
            MaxDiscountAmount: 30,
            MaxUses: 50,
            ValidFrom: DateTime.UtcNow.AddDays(-1),
            ValidUntil: DateTime.UtcNow.AddDays(30));

        var result = await handler.Handle(command, default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SUMMER10", result.GetDataOrThrow().Code);
        Assert.AreEqual(50, result.GetDataOrThrow().MaxUses);
        Assert.IsTrue(repo.Contains(result.GetDataOrThrow().Id));
    }

    [TestMethod]
    public async Task CreatePromoCode_NormalizesCodeToUpper()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new CreatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new CreatePromoCodeCommand("summer10", "Percentage", 10),
            default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("SUMMER10", result.GetDataOrThrow().Code);
    }

    [TestMethod]
    public async Task CreatePromoCode_DuplicateCode_ReturnsDuplicateCodeError()
    {
        var repo = new FakePromoCodeRepository();
        repo.Seed(PromoCodeFactory.Active("SAVE20"));
        var handler = new CreatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new CreatePromoCodeCommand("SAVE20", "Percentage", 10),
            default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.DuplicateCode.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task CreatePromoCode_InvalidPercentage_ReturnsError()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new CreatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new CreatePromoCodeCommand("TEST123", "Percentage", 0),
            default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.DiscountPercentRange.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task CreatePromoCode_InvalidDateRange_ReturnsError()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new CreatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new CreatePromoCodeCommand(
                "TEST123",
                "Fixed",
                10,
                ValidFrom: DateTime.UtcNow.AddDays(2),
                ValidUntil: DateTime.UtcNow.AddDays(1)),
            default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.DateRangeInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task CreatePromoCode_InvalidDiscountType_DefaultsToPercentage()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new CreatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new CreatePromoCodeCommand("TEST123", "NotAType", 15),
            default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Percentage", result.GetDataOrThrow().DiscountType);
        Assert.AreEqual(15m, result.GetDataOrThrow().DiscountValue);
    }

    [TestMethod]
    public async Task UpdatePromoCode_SetIsActiveFalse_UpdatesAggregate()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new UpdatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new UpdatePromoCodeCommand(promo.Id, IsActive: false),
            default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.GetDataOrThrow().IsActive);
    }

    [TestMethod]
    public async Task UpdatePromoCode_UnknownId_ReturnsPromoNotFound()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new UpdatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new UpdatePromoCodeCommand(Guid.NewGuid(), IsActive: false),
            default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.PromoNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdatePromoCode_ChangesDiscountAndLimits()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new UpdatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new UpdatePromoCodeCommand(
                promo.Id,
                DiscountType: "Fixed",
                DiscountValue: 5,
                MinimumOrderAmount: 25,
                MaxDiscountAmount: 10,
                MaxUses: 9),
            default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Fixed", result.GetDataOrThrow().DiscountType);
        Assert.AreEqual(5m, result.GetDataOrThrow().DiscountValue);
        Assert.AreEqual(25m, result.GetDataOrThrow().MinimumOrderAmount);
        Assert.AreEqual(10m, result.GetDataOrThrow().MaxDiscountAmount);
        Assert.AreEqual(9, result.GetDataOrThrow().MaxUses);
    }

    [TestMethod]
    public async Task UpdatePromoCode_InvalidDateRange_ReturnsError()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new UpdatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new UpdatePromoCodeCommand(
                promo.Id,
                ValidFrom: DateTime.UtcNow.AddDays(2),
                ValidUntil: DateTime.UtcNow.AddDays(1)),
            default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.DateRangeInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdatePromoCode_InvalidDiscountType_DefaultsToPercentage()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new UpdatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(
            new UpdatePromoCodeCommand(
                promo.Id,
                DiscountType: "Unknown",
                DiscountValue: 12),
            default);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Percentage", result.GetDataOrThrow().DiscountType);
        Assert.AreEqual(12m, result.GetDataOrThrow().DiscountValue);
    }

    [TestMethod]
    public async Task DeactivatePromoCode_SetsInactive()
    {
        var repo = new FakePromoCodeRepository();
        var promo = PromoCodeFactory.Active("SAVE20");
        repo.Seed(promo);
        var handler = new DeactivatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(new DeactivatePromoCodeCommand(promo.Id), default);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(repo.GetByIdAsync(promo.Id).Result!.IsActive);
    }

    [TestMethod]
    public async Task DeactivatePromoCode_UnknownId_ReturnsPromoNotFound()
    {
        var repo = new FakePromoCodeRepository();
        var handler = new DeactivatePromoCodeCommandHandler(repo);

        var result = await handler.Handle(new DeactivatePromoCodeCommand(Guid.NewGuid()), default);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(PromotionsErrors.PromoNotFound.Code, result.GetErrorOrThrow().Code);
    }
}
