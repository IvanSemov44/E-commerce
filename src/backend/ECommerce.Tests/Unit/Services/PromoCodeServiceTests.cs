using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class PromoCodeServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IRepository<PromoCode>> _mockPromoCodeRepository = null!;
    private Mock<ILogger<PromoCodeService>> _mockLogger = null!;
    private Mock<IMapper> _mockMapper = null!;
    private PromoCodeService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockPromoCodeRepository = MockHelpers.CreateMockRepository<PromoCode>();
        _mockLogger = MockHelpers.CreateMockLogger<PromoCodeService>();
        _mockMapper = MockHelpers.CreateMockMapper();

        // Setup repository on UnitOfWork
        _mockUnitOfWork.Setup(u => u.PromoCodes).Returns(_mockPromoCodeRepository.Object);

        _service = new PromoCodeService(_mockUnitOfWork.Object, _mockMapper.Object, _mockLogger.Object);
    }

    #region GetAllAsync Tests

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllPromoCodes()
    {
        // Arrange
        var promoCodes = new List<PromoCode>
        {
            TestDataFactory.CreatePromoCode("SAVE10"),
            TestDataFactory.CreatePromoCode("SAVE20"),
            TestDataFactory.CreatePromoCode("SAVE30")
        };
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(promoCodes);

        // Setup mapper to map list
        _mockMapper.Setup(m => m.Map<List<PromoCodeDto>>(It.IsAny<List<PromoCode>>()))
            .Returns((List<PromoCode> source) => source.Select(p => new PromoCodeDto
            {
                Id = p.Id,
                Code = p.Code,
                DiscountType = p.DiscountType,
                DiscountValue = p.DiscountValue,
                IsActive = p.IsActive
            }).ToList());

        // Act
        var result = await _service.GetAllAsync(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [TestMethod]
    public async Task GetAllAsync_WithSearchFilter_ReturnsMatching()
    {
        // Arrange
        var promoCodes = new List<PromoCode>
        {
            TestDataFactory.CreatePromoCode("SAVE10"),
            TestDataFactory.CreatePromoCode("SUMMER20"),
            TestDataFactory.CreatePromoCode("WINTER30")
        };
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(promoCodes);

        _mockMapper.Setup(m => m.Map<List<PromoCodeDto>>(It.IsAny<IEnumerable<PromoCode>>()))
            .Returns((IEnumerable<PromoCode> source) => source.Select(p => new PromoCodeDto
            {
                Id = p.Id,
                Code = p.Code
            }).ToList());

        // Act
        var result = await _service.GetAllAsync(page: 1, pageSize: 10, search: "SAVE");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Contain("SAVE");
    }

    [TestMethod]
    public async Task GetAllAsync_WithActiveFilter_ReturnsActiveOnly()
    {
        // Arrange
        var promoCodes = new List<PromoCode>
        {
            TestDataFactory.CreatePromoCode("ACTIVE1", isActive: true),
            TestDataFactory.CreatePromoCode("ACTIVE2", isActive: true),
            TestDataFactory.CreatePromoCode("INACTIVE", isActive: false)
        };
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(promoCodes);

        _mockMapper.Setup(m => m.Map<List<PromoCodeDto>>(It.IsAny<IEnumerable<PromoCode>>()))
            .Returns((IEnumerable<PromoCode> source) => source.Select(p => new PromoCodeDto
            {
                Id = p.Id,
                Code = p.Code,
                IsActive = p.IsActive
            }).ToList());

        // Act
        var result = await _service.GetAllAsync(page: 1, pageSize: 10, isActive: true);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_ExistingId_ReturnsPromoCode()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode("TEST10");
        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(promoCode.Id, It.IsAny<bool>()))
            .ReturnsAsync(promoCode);

        _mockMapper.Setup(m => m.Map<PromoCodeDetailDto>(It.IsAny<PromoCode>()))
            .Returns(new PromoCodeDetailDto
            {
                Id = promoCode.Id,
                Code = promoCode.Code,
                DiscountType = promoCode.DiscountType,
                DiscountValue = promoCode.DiscountValue
            });

        // Act
        var result = await _service.GetByIdAsync(promoCode.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("TEST10");
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>()))
            .ReturnsAsync((PromoCode?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [TestMethod]
    public async Task CreateAsync_ValidData_CreatesPromoCode()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "NEWCODE",
            DiscountType = "percentage",
            DiscountValue = 15,
            MinOrderAmount = 50,
            IsActive = true
        };

        _mockPromoCodeRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<PromoCode>());

        _mockMapper.Setup(m => m.Map<PromoCode>(It.IsAny<CreatePromoCodeDto>()))
            .Returns(new PromoCode
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderAmount = dto.MinOrderAmount,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        _mockMapper.Setup(m => m.Map<PromoCodeDetailDto>(It.IsAny<PromoCode>()))
            .Returns((PromoCode source) => new PromoCodeDetailDto
            {
                Id = source.Id,
                Code = source.Code,
                DiscountType = source.DiscountType,
                DiscountValue = source.DiscountValue
            });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("NEWCODE");
        _mockPromoCodeRepository.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateAsync_DuplicateCode_ThrowsPromoCodeAlreadyExistsException()
    {
        // Arrange
        var existing = TestDataFactory.CreatePromoCode("DUPLICATE");
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<PromoCode> { existing });

        var dto = new CreatePromoCodeDto
        {
            Code = "DUPLICATE",
            DiscountType = "percentage",
            DiscountValue = 10
        };

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<PromoCodeAlreadyExistsException>();
    }

    [TestMethod]
    public async Task CreateAsync_InvalidPercentage_ThrowsInvalidPromoCodeConfigurationException()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "INVALID",
            DiscountType = "percentage",
            DiscountValue = 150 // Invalid: > 100
        };
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<PromoCode>());

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidPromoCodeConfigurationException>();
    }

    #endregion

    #region ValidatePromoCodeAsync Tests

    [TestMethod]
    public async Task ValidatePromoCodeAsync_ValidCode_ReturnsSuccess()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode(
            "VALID20",
            discountType: "percentage",
            discountValue: 20,
            isActive: true);

        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<PromoCode> { promoCode });

        _mockMapper.Setup(m => m.Map<PromoCodeDto>(It.IsAny<PromoCode>()))
            .Returns(new PromoCodeDto
            {
                Id = promoCode.Id,
                Code = promoCode.Code,
                DiscountType = promoCode.DiscountType,
                DiscountValue = promoCode.DiscountValue
            });

        // Act
        var result = await _service.ValidatePromoCodeAsync("VALID20", orderAmount: 100);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(20); // 20% of 100
        result.Message.Should().Be("Promo code applied successfully");
    }

    [TestMethod]
    public async Task ValidatePromoCodeAsync_NonExistentCode_ReturnsInvalid()
    {
        // Arrange
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<PromoCode>());

        // Act
        var result = await _service.ValidatePromoCodeAsync("NOTFOUND", orderAmount: 100);

        // Assert
        result.IsValid.Should().BeFalse();
        result.DiscountAmount.Should().Be(0);
        result.Message.Should().Be("Promo code not found");
    }

    [TestMethod]
    public async Task ValidatePromoCodeAsync_InactiveCode_ReturnsInvalid()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode("INACTIVE", isActive: false);
        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<PromoCode> { promoCode });

        // Act
        var result = await _service.ValidatePromoCodeAsync("INACTIVE", orderAmount: 100);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("This promo code is no longer active");
    }

    [TestMethod]
    public async Task ValidatePromoCodeAsync_ExpiredCode_ReturnsInvalid()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode(
            "EXPIRED",
            isActive: true,
            endDate: DateTime.UtcNow.AddDays(-1));

        _mockPromoCodeRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<PromoCode> { promoCode });

        // Act
        var result = await _service.ValidatePromoCodeAsync("EXPIRED", orderAmount: 100);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("This promo code has expired");
    }

    [TestMethod]
    public async Task ValidatePromoCodeAsync_PercentageDiscount_CalculatesCorrectly()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode(
            "PERCENT25",
            discountType: "percentage",
            discountValue: 25,
            isActive: true);

        _mockPromoCodeRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<PromoCode> { promoCode });

        _mockMapper.Setup(m => m.Map<PromoCodeDto>(It.IsAny<PromoCode>()))
            .Returns(new PromoCodeDto { Id = promoCode.Id, Code = promoCode.Code });

        // Act
        var result = await _service.ValidatePromoCodeAsync("PERCENT25", orderAmount: 200);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(50); // 25% of 200
    }

    #endregion

    #region IncrementUsedCountAsync Tests

    [TestMethod]
    public async Task IncrementUsedCountAsync_ValidCode_IncrementsCount()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode("INCREMENT", usedCount: 5);
        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(promoCode.Id, It.IsAny<bool>()))
            .ReturnsAsync(promoCode);

        // Act
        await _service.IncrementUsedCountAsync(promoCode.Id);

        // Assert
        promoCode.UsedCount.Should().Be(6);
        _mockPromoCodeRepository.Verify(r => r.UpdateAsync(It.IsAny<PromoCode>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task IncrementUsedCountAsync_AtMaxUses_ThrowsException()
    {
        // Arrange
        var promoCode = TestDataFactory.CreatePromoCode("MAXED", maxUses: 10, usedCount: 10);
        _mockPromoCodeRepository.Setup(r => r.GetByIdAsync(promoCode.Id, It.IsAny<bool>()))
            .ReturnsAsync(promoCode);

        // Act
        Func<Task> act = async () => await _service.IncrementUsedCountAsync(promoCode.Id);

        // Assert
        await act.Should().ThrowAsync<PromoCodeUsageLimitReachedException>();
    }

    #endregion
}
