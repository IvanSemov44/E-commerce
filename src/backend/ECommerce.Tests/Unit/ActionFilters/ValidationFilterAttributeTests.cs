using ECommerce.API.ActionFilters;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Contracts.DTOs.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Tests.Unit.ActionFilters;

/// <summary>
/// Unit tests for ValidationFilterAttribute.
/// Tests that the filter properly validates DTOs and returns standardized ApiResponse errors.
/// </summary>
[TestClass]
public class ValidationFilterAttributeTests
{
    private ValidationFilterAttribute _filter = null!;
    private DefaultHttpContext _httpContext = null!;
    private RouteData _routeData = null!;
    private ActionDescriptor _actionDescriptor = null!;

    [TestInitialize]
    public void Setup()
    {
        _filter = new ValidationFilterAttribute();
        _httpContext = new DefaultHttpContext();
        _routeData = new RouteData();
        _routeData.Values["controller"] = "TestController";
        _routeData.Values["action"] = "TestAction";
        _actionDescriptor = new ActionDescriptor();
    }

    [TestMethod]
    public void OnActionExecuting_WhenDtoIsNull_ReturnsBadRequestWithNullMessage()
    {
        // Arrange
        var actionArguments = new Dictionary<string, object?> { { "request", null } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldNotBeNull();
        context.Result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)context.Result;
        badRequestResult.StatusCode.ShouldBe(400);
    }

    [TestMethod]
    public void OnActionExecuting_WhenDtoIsNull_IncludesControllerAndActionInErrorMessage()
    {
        // Arrange
        var actionArguments = new Dictionary<string, object?> { { "request", null } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        var badRequestResult = (BadRequestObjectResult)context.Result!;
        var errorResponse = badRequestResult.Value as ApiResponse<object>;
        errorResponse?.ErrorDetails?.Message.ShouldContain("Object is null");
        errorResponse?.ErrorDetails?.Message.ShouldContain("TestController");
        errorResponse?.ErrorDetails?.Message.ShouldContain("TestAction");
    }

    [TestMethod]
    public void OnActionExecuting_WhenModelStateInvalid_ReturnsUnprocessableEntity()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "",
            Price = -10,
            StockQuantity = -5,
            CategoryId = Guid.Empty
        };

        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Add ModelState errors
        context.ModelState.AddModelError("Name", "Name is required");
        context.ModelState.AddModelError("Price", "Price must be greater than 0");

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldNotBeNull();
        context.Result.ShouldBeOfType<UnprocessableEntityObjectResult>();
        var result = (UnprocessableEntityObjectResult)context.Result;
        result.StatusCode.ShouldBe(422);
    }

    [TestMethod]
    public void OnActionExecuting_WhenModelStateInvalid_IncludesAllErrors()
    {
        // Arrange
        var dto = new CreateProductDto();
        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        context.ModelState.AddModelError("Name", "Name is required");
        context.ModelState.AddModelError("Price", "Price must be greater than 0");
        context.ModelState.AddModelError("CategoryId", "Category is required");

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        var result = (UnprocessableEntityObjectResult)context.Result!;
        var errorResponse = result.Value as ApiResponse<object>;
        var errors = errorResponse?.ErrorDetails?.Errors?.Values.SelectMany(e => e).ToList();
        errors.Count.ShouldBe(3);
        errors.ShouldContain("Name is required");
        errors.ShouldContain("Price must be greater than 0");
        errors.ShouldContain("Category is required");
    }

    [TestMethod]
    public void OnActionExecuting_WhenModelStateValid_DoesNotSetResult()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Valid Product",
            Price = 99.99m,
            StockQuantity = 100,
            CategoryId = Guid.NewGuid()
        };

        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeNull();
    }

    [TestMethod]
    public void OnActionExecuting_WhenMultipleDtoArguments_IdentifiesFirstDtoParameter()
    {
        // Arrange
        var dto = new CreateProductDto();
        var actionArguments = new Dictionary<string, object?>
        {
            { "productDto", dto },
            { "userId", Guid.NewGuid() }
        };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Add error to ModelState
        context.ModelState.AddModelError("Name", "Required");

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [TestMethod]
    public void OnActionExecuting_WhenNoDtoParameter_ReturnsBadRequest()
    {
        // Arrange
        var actionArguments = new Dictionary<string, object?> { { "id", Guid.NewGuid() } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public void OnActionExecuting_WhenEmptyModelState_DoesNotSetResult()
    {
        // Arrange
        var dto = new CreateProductDto();
        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act (ModelState has no errors by default)
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeNull();
    }

    [TestMethod]
    public void OnActionExecuting_WhenModelStateHasMultipleErrorsPerKey_IncludesAllMessages()
    {
        // Arrange
        var dto = new CreateProductDto();
        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        context.ModelState.AddModelError("Price", "Price is required");
        context.ModelState.AddModelError("Price", "Price must be positive");

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        var result = (UnprocessableEntityObjectResult)context.Result!;
        var errorResponse = result.Value as ApiResponse<object>;
        var errors = errorResponse?.ErrorDetails?.Errors?.Values.SelectMany(e => e).ToList();
        errors.ShouldContain("Price is required");
        errors.ShouldContain("Price must be positive");
    }

    [TestMethod]
    public void OnActionExecuting_WhenActionArgumentsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var actionArguments = new Dictionary<string, object?>();
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public void OnActionExecuting_WithNullDtoReference_ReturnsBadRequest()
    {
        // Arrange
        object? nullDto = null;
        var actionArguments = new Dictionary<string, object?> { { "createProductDto", nullDto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public void OnActionExecuted_DoesNotThrow()
    {
        // Arrange
        var context = new ActionExecutedContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            controller: new object());

        // Act & Assert - should not throw
        _filter.OnActionExecuted(context);
    }

    [TestMethod]
    public void OnActionExecuting_ReturnsApiResponseErrorType()
    {
        // Arrange
        var dto = new CreateProductDto();
        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        context.ModelState.AddModelError("Name", "Required");

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        var result = (UnprocessableEntityObjectResult)context.Result!;
        var errorResponse = result.Value as ApiResponse<object>;
        errorResponse.ShouldNotBeNull();
        errorResponse?.Success.ShouldBeFalse();
    }

    [TestMethod]
    public void OnActionExecuting_WhenDtoPrefixedWithCreate_IdentifiesDtoCorrectly()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test" };
        var actionArguments = new Dictionary<string, object?> { { "createProductDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeNull(); // Valid DTO, no errors
    }

    [TestMethod]
    public void OnActionExecuting_WhenDtoPrefixedWithUpdate_IdentifiesDtoCorrectly()
    {
        // Arrange
        var dto = new CreateProductDto(); // Using similar structure for test
        var actionArguments = new Dictionary<string, object?> { { "updateProductDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        context.Result.ShouldBeNull(); // Valid DTO, no errors
    }

    [TestMethod]
    public void OnActionExecuting_WhenValidationFails_ErrorResponseHasCorrectStatusCode()
    {
        // Arrange
        var dto = new CreateProductDto();
        var actionArguments = new Dictionary<string, object?> { { "productDto", dto } };
        var context = new ActionExecutingContext(
            new ActionContext(_httpContext, _routeData, _actionDescriptor),
            new List<IFilterMetadata>(),
            actionArguments,
            controller: new object());

        context.ModelState.AddModelError("Name", "Name is required");

        // Act
        _filter.OnActionExecuting(context);

        // Assert
        var result = (UnprocessableEntityObjectResult)context.Result!;
        result.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }
}

