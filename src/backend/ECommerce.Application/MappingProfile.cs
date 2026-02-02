using AutoMapper;
using ECommerce.Core.Entities;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Wishlist;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.DTOs;

namespace ECommerce.Application;

/// <summary>
/// AutoMapper configuration for entity-to-DTO and DTO-to-entity mappings.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ReverseMap();

        // User Profile mappings
        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<UpdateProfileDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                src.Reviews.Any() ? src.Reviews.Average(r => (decimal)r.Rating) : 0))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.Reviews.Count))
            .ReverseMap();

        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                src.Reviews.Any() ? src.Reviews.Average(r => (decimal)r.Rating) : 0))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.Reviews.Count))
            .ReverseMap();

        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        // Category mappings (from Products folder)
        CreateMap<Category, ProductCategoryDto>()
            .ReverseMap();

        // Category mappings (from new DTOs folder)
        CreateMap<Category, DTOs.CategoryDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.Ignore());

        CreateMap<Category, CategoryDetailDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.Parent))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children));

        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // ProductImage mappings
        CreateMap<ProductImage, ProductImageDto>()
            .ReverseMap();

        // Review mappings (product-embedded simplified DTOs)
        CreateMap<Review, ProductReviewDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Anonymous"))
            .ReverseMap();

        CreateMap<Review, ReviewDetailDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : "Anonymous"));

        CreateMap<CreateReviewDto, Review>();
        CreateMap<UpdateReviewDto, Review>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Order mappings
        CreateMap<Order, OrderDto>()
            .ReverseMap();

        CreateMap<Order, OrderDetailDto>()
            .ReverseMap();

        CreateMap<OrderItem, OrderItemDto>()
            .ReverseMap();

        // Cart mappings
        CreateMap<Cart, CartDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ReverseMap();

        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Product != null ? src.Product.Id : src.ProductId))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
            .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src =>
                src.Product != null
                    ? (src.Product.Images.FirstOrDefault(x => x.IsPrimary) != null
                        ? src.Product.Images.FirstOrDefault(x => x.IsPrimary)!.Url
                        : src.Product.Images.FirstOrDefault() != null
                            ? src.Product.Images.FirstOrDefault()!.Url
                            : null)
                    : null))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0m))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => (src.Product != null ? src.Product.Price : 0m) * src.Quantity));

        // Address mappings
        CreateMap<Address, AddressDto>()
            .ReverseMap();

        // Wishlist mappings
        CreateMap<Wishlist, WishlistDto>()
            .ReverseMap();

        // PromoCode mappings
        CreateMap<PromoCode, PromoCodeDto>();
        CreateMap<PromoCode, PromoCodeDetailDto>();
        CreateMap<CreatePromoCodeDto, PromoCode>();
        CreateMap<UpdatePromoCodeDto, PromoCode>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Inventory mappings
        CreateMap<Product, InventoryDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.LowStockThreshold, opt => opt.MapFrom(src => src.LowStockThreshold))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Images.FirstOrDefault() != null ? src.Images.FirstOrDefault()!.Url : null))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price));

        CreateMap<Product, LowStockAlertDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku))
            .ForMember(dest => dest.CurrentStock, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.LowStockThreshold, opt => opt.MapFrom(src => src.LowStockThreshold));

        CreateMap<InventoryLog, InventoryLogDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.QuantityChange, opt => opt.MapFrom(src => src.QuantityChange))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.ReferenceId, opt => opt.MapFrom(src => src.ReferenceId))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.StockAfterChange, opt => opt.Ignore());
    }
}
