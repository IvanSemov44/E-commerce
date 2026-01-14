using AutoMapper;
using ECommerce.Core.Entities;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Cart;

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
            .ReverseMap();

        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews))
            .ReverseMap();

        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ReverseMap();

        // ProductImage mappings
        CreateMap<ProductImage, ProductImageDto>()
            .ReverseMap();

        // Review mappings
        CreateMap<Review, ReviewDto>()
            .ReverseMap();

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
    }
}
