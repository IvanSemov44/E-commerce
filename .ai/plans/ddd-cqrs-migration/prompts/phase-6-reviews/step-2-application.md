# Phase 6, Step 2: Application Project

**Prerequisite**: Step 1 (Domain) complete and building.

Create `ECommerce.Reviews.Application` — DTOs, commands, queries, and handlers for the Reviews bounded context.

---

## Task 1: Create the project

```bash
cd src/backend
dotnet new classlib -n ECommerce.Reviews.Application -o ECommerce.Reviews.Application
dotnet sln ECommerce.sln add ECommerce.Reviews.Application/ECommerce.Reviews.Application.csproj
dotnet add ECommerce.Reviews.Application/ECommerce.Reviews.Application.csproj reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj
dotnet add ECommerce.Reviews.Application/ECommerce.Reviews.Application.csproj reference ECommerce.Reviews.Domain/ECommerce.Reviews.Domain.csproj
dotnet add ECommerce.Reviews.Application/ECommerce.Reviews.Application.csproj package MediatR
rm ECommerce.Reviews.Application/Class1.cs
```

---

## Task 2: Define DTOs

**File: `ECommerce.Reviews.Application/DTOs/ReviewDto.cs`**

```csharp
namespace ECommerce.Reviews.Application.DTOs;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int HelpfulCount { get; set; }
    public int FlagCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**File: `ECommerce.Reviews.Application/DTOs/ReviewDetailDto.cs`**

```csharp
namespace ECommerce.Reviews.Application.DTOs;

public class ReviewDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int HelpfulCount { get; set; }
    public int FlagCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Task 3: Define Commands

**File: `ECommerce.Reviews.Application/Commands/CreateReviewCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record CreateReviewCommand(
    Guid ProductId,
    Guid AuthorId,
    string? AuthorName,
    int Rating,
    string Text) : IRequest<Result<ReviewDetailDto>>, ITransactionalCommand;
```

**File: `ECommerce.Reviews.Application/Commands/UpdateReviewCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record UpdateReviewCommand(
    Guid ReviewId,
    int? Rating,
    string? Text) : IRequest<Result<ReviewDetailDto>>, ITransactionalCommand;
```

**File: `ECommerce.Reviews.Application/Commands/DeleteReviewCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record DeleteReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
```

**File: `ECommerce.Reviews.Application/Commands/MarkReviewHelpfulCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record MarkReviewHelpfulCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
```

**File: `ECommerce.Reviews.Application/Commands/FlagReviewCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record FlagReviewCommand(Guid ReviewId, string? Reason) : IRequest<Result>, ITransactionalCommand;
```

**File: `ECommerce.Reviews.Application/Commands/ApproveReviewCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record ApproveReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
```

**File: `ECommerce.Reviews.Application/Commands/RejectReviewCommand.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Commands;

public record RejectReviewCommand(Guid ReviewId) : IRequest<Result>, ITransactionalCommand;
```

---

## Task 4: Define Queries

**File: `ECommerce.Reviews.Application/Queries/GetReviewByIdQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetReviewByIdQuery(Guid ReviewId) : IRequest<Result<ReviewDetailDto>>;
```

**File: `ECommerce.Reviews.Application/Queries/GetProductReviewsQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetProductReviewsQuery(
    Guid ProductId,
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDto>>>;
```

**File: `ECommerce.Reviews.Application/Queries/GetAllReviewsQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetAllReviewsQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Status) : IRequest<Result<PaginatedResult<ReviewDto>>>;
```

**File: `ECommerce.Reviews.Application/Queries/GetPendingReviewsQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetPendingReviewsQuery(
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDto>>>;
```

**File: `ECommerce.Reviews.Application/Queries/GetFlaggedReviewsQuery.cs`**

```csharp
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.Queries;

public record GetFlaggedReviewsQuery(
    int Page,
    int PageSize) : IRequest<Result<PaginatedResult<ReviewDto>>>;
```

---

## Task 5: Define Handlers

**File: `ECommerce.Reviews.Application/CommandHandlers/CreateReviewCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<ReviewDetailDto>>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReviewCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReviewDetailDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate
        var existing = await _repository.GetByProductAndAuthorAsync(request.ProductId, request.AuthorId, cancellationToken);
        if (existing is not null)
            return Result<ReviewDetailDto>.Failure(ReviewsErrors.DuplicateReview);

        // Validate rating
        var ratingResult = Rating.Create(request.Rating);
        if (!ratingResult.IsSuccess)
            return Result<ReviewDetailDto>.Failure(ratingResult.Error!);

        // Validate text
        var textResult = ReviewText.Create(request.Text);
        if (!textResult.IsSuccess)
            return Result<ReviewDetailDto>.Failure(textResult.Error!);

        // Create aggregate
        var createResult = Review.Create(
            request.ProductId,
            request.AuthorId,
            request.AuthorName,
            ratingResult.Value!,
            textResult.Value!);

        if (!createResult.IsSuccess)
            return Result<ReviewDetailDto>.Failure(createResult.Error!);

        var review = createResult.Value!;
        await _repository.UpsertAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReviewDetailDto>.Ok(review.ToDetailDto());
    }
}
```

**File: `ECommerce.Reviews.Application/CommandHandlers/UpdateReviewCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Aggregates.Review;
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.Reviews.Domain.ValueObjects;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, Result<ReviewDetailDto>>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateReviewCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReviewDetailDto>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result<ReviewDetailDto>.Failure(ReviewsErrors.ReviewNotFound);

        Rating? newRating = null;
        if (request.Rating.HasValue)
        {
            var ratingResult = Rating.Create(request.Rating.Value);
            if (!ratingResult.IsSuccess)
                return Result<ReviewDetailDto>.Failure(ratingResult.Error!);
            newRating = ratingResult.Value;
        }

        ReviewText? newText = null;
        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            var textResult = ReviewText.Create(request.Text);
            if (!textResult.IsSuccess)
                return Result<ReviewDetailDto>.Failure(textResult.Error!);
            newText = textResult.Value;
        }

        var updateResult = review.Update(newRating, newText);
        if (!updateResult.IsSuccess)
            return Result<ReviewDetailDto>.Failure(updateResult.Error!);

        await _repository.UpsertAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReviewDetailDto>.Ok(review.ToDetailDto());
    }
}
```

**File: `ECommerce.Reviews.Application/CommandHandlers/DeleteReviewCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReviewCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Failure(ReviewsErrors.ReviewNotFound);

        await _repository.DeleteAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

**File: `ECommerce.Reviews.Application/CommandHandlers/MarkReviewHelpfulCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class MarkReviewHelpfulCommandHandler : IRequestHandler<MarkReviewHelpfulCommand, Result>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkReviewHelpfulCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkReviewHelpfulCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Failure(ReviewsErrors.ReviewNotFound);

        review.MarkAsHelpful();
        await _repository.UpsertAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

**File: `ECommerce.Reviews.Application/CommandHandlers/FlagReviewCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class FlagReviewCommandHandler : IRequestHandler<FlagReviewCommand, Result>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public FlagReviewCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(FlagReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Failure(ReviewsErrors.ReviewNotFound);

        review.Flag();
        await _repository.UpsertAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

**File: `ECommerce.Reviews.Application/CommandHandlers/ApproveReviewCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class ApproveReviewCommandHandler : IRequestHandler<ApproveReviewCommand, Result>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveReviewCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Failure(ReviewsErrors.ReviewNotFound);

        var result = review.Approve();
        if (!result.IsSuccess)
            return result;

        await _repository.UpsertAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

**File: `ECommerce.Reviews.Application/CommandHandlers/RejectReviewCommandHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.CommandHandlers;

public class RejectReviewCommandHandler : IRequestHandler<RejectReviewCommand, Result>
{
    private readonly IReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectReviewCommandHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RejectReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result.Failure(ReviewsErrors.ReviewNotFound);

        review.Reject();
        await _repository.UpsertAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
```

**File: `ECommerce.Reviews.Application/QueryHandlers/GetProductReviewsQueryHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, Result<PaginatedResult<ReviewDto>>>
{
    private readonly IReviewRepository _repository;

    public GetProductReviewsQueryHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaginatedResult<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetByProductAsync(
            request.ProductId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(r => r.ToDto()).ToList();
        var result = new PaginatedResult<ReviewDto>(dtos, request.Page, request.PageSize, total);

        return Result<PaginatedResult<ReviewDto>>.Ok(result);
    }
}
```

**File: `ECommerce.Reviews.Application/QueryHandlers/GetAllReviewsQueryHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetAllReviewsQueryHandler : IRequestHandler<GetAllReviewsQuery, Result<PaginatedResult<ReviewDto>>>
{
    private readonly IReviewRepository _repository;

    public GetAllReviewsQueryHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaginatedResult<ReviewDto>>> Handle(GetAllReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetAllAsync(
            request.Page, request.PageSize, request.Search, request.Status, cancellationToken);

        var dtos = items.Select(r => r.ToDto()).ToList();
        var result = new PaginatedResult<ReviewDto>(dtos, request.Page, request.PageSize, total);

        return Result<PaginatedResult<ReviewDto>>.Ok(result);
    }
}
```

**File: `ECommerce.Reviews.Application/QueryHandlers/GetPendingReviewsQueryHandler.cs`**

```csharp
using ECommerce.Reviews.Domain.Interfaces;
using ECommerce.SharedKernel;
using MediatR;

namespace ECommerce.Reviews.Application.QueryHandlers;

public class GetPendingReviewsQueryHandler : IRequestHandler<GetPendingReviewsQuery, Result<PaginatedResult<ReviewDto>>>
{
    private readonly IReviewRepository _repository;

    public GetPendingReviewsQueryHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaginatedResult<ReviewDto>>> Handle(GetPendingReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPendingAsync(request.Page, request.PageSize, cancellationToken);
        var dtos = items.Select(r => r.ToDto()).ToList();
        var result = new PaginatedResult<ReviewDto>(dtos, request.Page, request.PageSize, total);

        return Result<PaginatedResult<ReviewDto>>.Ok(result);
    }
}
```

---

## Task 6: Mapping Extensions

**File: `ECommerce.Reviews.Application/Mappings/ReviewsMappingExtensions.cs`**

```csharp
using ECommerce.Reviews.Domain.Aggregates.Review;

namespace ECommerce.Reviews.Application.Mappings;

public static class ReviewsMappingExtensions
{
    public static ReviewDto ToDto(this Review review) => new()
    {
        Id = review.Id,
        ProductId = review.ProductId,
        AuthorId = review.AuthorId,
        AuthorName = review.AuthorName,
        Rating = review.Rating.Value,
        Text = review.Text.Value,
        Status = review.Status.ToString(),
        HelpfulCount = review.HelpfulCount,
        FlagCount = review.FlagCount,
        CreatedAt = review.CreatedAt
    };

    public static ReviewDetailDto ToDetailDto(this Review review) => new()
    {
        Id = review.Id,
        ProductId = review.ProductId,
        AuthorId = review.AuthorId,
        AuthorName = review.AuthorName,
        Rating = review.Rating.Value,
        Text = review.Text.Value,
        Status = review.Status.ToString(),
        HelpfulCount = review.HelpfulCount,
        FlagCount = review.FlagCount,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt
    };
}
```

---

## Task 7: DependencyInjection

**File: `ECommerce.Reviews.Application/DependencyInjection.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection));
        return services;
    }
}
```

---

## Acceptance Criteria

- [ ] Project builds with zero errors
- [ ] DTOs defined: `ReviewDto`, `ReviewDetailDto`
- [ ] 7 commands defined: Create, Update, Delete, MarkHelpful, Flag, Approve, Reject
- [ ] 5 queries defined: GetById, GetProductReviews, GetAllReviews, GetPending, GetFlagged
- [ ] 7 command handlers implement `IRequestHandler<TCommand, Result<T>>`
- [ ] 4 query handlers implement `IRequestHandler<TQuery, Result<PaginatedResult<ReviewDto>>>`
- [ ] `ToDto()` and `ToDetailDto()` mapping extensions work correctly
- [ ] All commands implement `ITransactionalCommand`
- [ ] DependencyInjection registers MediatR handlers
