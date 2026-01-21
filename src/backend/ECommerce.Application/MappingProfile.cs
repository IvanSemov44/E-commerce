using AutoMapper;
using ECommerce.Core.Entities;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.DTOs.Wishlist;
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
        CreateMap<Category, DTOs.Products.CategoryDto>()
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

        // Review mappings
        CreateMap<Review, ReviewDto>()
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
            .ReverseMap();

        CreateMap<CartItem, CartItemDto>()
            .ReverseMap();

        // Address mappings
        CreateMap<Address, AddressDto>()
            .ReverseMap();

        // Wishlist mappings
        CreateMap<Wishlist, WishlistDto>()
            .ReverseMap();
    }
}
