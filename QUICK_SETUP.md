# Quick Setup Guide

## Files Created

### Service Interfaces
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Core\Interfaces\Services\IAuthService.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Core\Interfaces\Services\IProductService.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Core\Interfaces\Services\IOrderService.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Core\Interfaces\Services\ICartService.cs`

### Service Implementations
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Application\Services\AuthService.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Application\Services\ProductService.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Application\Services\OrderService.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Application\Services\CartService.cs`

### Controllers
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.API\Controllers\AuthController.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.API\Controllers\ProductsController.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.API\Controllers\OrdersController.cs`
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.API\Controllers\CartController.cs`

### Repository Interface
- `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Core\Interfaces\Repositories\IUnitOfWork.cs`

---

## Step 1: Install Required NuGet Package

Add BCrypt for password hashing to ECommerce.Application.csproj:

```bash
cd c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Application
dotnet add package BCrypt.Net-Next --version 4.0.3
```

Add JWT packages to ECommerce.API.csproj:

```bash
cd c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
```

---

## Step 2: Create AutoMapper Profile

Create file: `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Application\Mappings\MappingProfile.cs`

```csharp
using AutoMapper;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Products;
using ECommerce.Core.Entities;

namespace ECommerce.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Product Mappings
        CreateMap<Product, ProductDto>();
        CreateMap<Product, ProductDetailDto>();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<Category, CategoryDto>();
        CreateMap<Review, ReviewDto>();

        // User Mappings
        CreateMap<User, UserDto>();

        // Order Mappings
        CreateMap<Order, OrderDto>();
        CreateMap<Order, OrderDetailDto>();
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<AddressDto, Address>();
        CreateMap<Address, AddressDto>();

        // Cart Mappings
        CreateMap<Cart, CartDto>();
        CreateMap<CartItem, CartItemDto>();
    }
}
```

---

## Step 3: Create UnitOfWork Implementation

Create file: `c:\Users\ivans\Desktop\Dev\E-commerce\src\backend\ECommerce.Infrastructure\UnitOfWork.cs`

```csharp
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IRepository<User>? _users;
    private IRepository<Product>? _products;
    private IRepository<Order>? _orders;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<Cart>? _carts;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Category>? _categories;
    private IRepository<Review>? _reviews;
    private IRepository<Address>? _addresses;
    private IRepository<PromoCode>? _promoCodes;
    private IRepository<InventoryLog>? _inventoryLogs;
    private IRepository<Wishlist>? _wishlists;
    private IRepository<ProductImage>? _productImages;
    private IProductRepository? _productRepository;
    private IUserRepository? _userRepository;
    private IOrderRepository? _orderRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
    public IRepository<Order> Orders => _orders ??= new Repository<Order>(_context);
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<Cart> Carts => _carts ??= new Repository<Cart>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Review> Reviews => _reviews ??= new Repository<Review>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<PromoCode> PromoCodes => _promoCodes ??= new Repository<PromoCode>(_context);
    public IRepository<InventoryLog> InventoryLogs => _inventoryLogs ??= new Repository<InventoryLog>(_context);
    public IRepository<Wishlist> Wishlists => _wishlists ??= new Repository<Wishlist>(_context);
    public IRepository<ProductImage> ProductImages => _productImages ??= new Repository<ProductImage>(_context);

    public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);
    public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);
    public IOrderRepository OrderRepository => _orderRepository ??= new OrderRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

---

## Step 4: Update Program.cs

Add the following to your Program.cs (after builder.Services):

```csharp
using ECommerce.Application.Mappings;
using ECommerce.Application.Services;
using ECommerce.Core.Interfaces.Services;
using ECommerce.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Add Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Enable middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## Step 5: Update appsettings.json

Add the following configuration:

```json
{
  "Jwt": {
    "Key": "your-super-secret-key-that-is-at-least-32-characters-long-change-this",
    "Issuer": "YourECommerceAPI",
    "Audience": "YourECommerceClients",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

---

## Step 6: Add Missing Repository Methods

Update your IRepository interface to include these helper methods used by services:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> SaveChangesAsync();
    Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    Task Add(T entity); // Used in some services
}
```

---

## Step 7: Build and Test

```bash
cd c:\Users\ivans\Desktop\Dev\E-commerce\src\backend
dotnet build
```

---

## Testing the API

### 1. Register a User
```bash
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

### 2. Login
```bash
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

### 3. Get All Products
```bash
GET https://localhost:5001/api/products?page=1&pageSize=20
```

### 4. Get Cart (With JWT Token)
```bash
GET https://localhost:5001/api/cart
Authorization: Bearer {token_from_login}
```

### 5. Add to Cart
```bash
POST https://localhost:5001/api/cart/items
Authorization: Bearer {token}
Content-Type: application/json

{
  "productId": "product-guid",
  "quantity": 2
}
```

### 6. Create Order
```bash
POST https://localhost:5001/api/orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "shippingAddress": {
    "firstName": "John",
    "lastName": "Doe",
    "streetLine1": "123 Main St",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "USA"
  },
  "paymentMethod": "Credit Card"
}
```

---

## Troubleshooting

### Issue: JWT Configuration Missing
**Solution**: Ensure JWT settings are in appsettings.json and not empty

### Issue: BCrypt Not Found
**Solution**: Run `dotnet add package BCrypt.Net-Next`

### Issue: AutoMapper Not Working
**Solution**: Ensure MappingProfile is created and registered in Program.cs

### Issue: DbContext Not Found
**Solution**: Create ApplicationDbContext in Infrastructure project with DbSet for all entities

### Issue: Repository Methods Not Found
**Solution**: Implement IRepository<T> base repository class and specific repository classes

---

## Next Steps

1. **Implement Database Context**: Create ApplicationDbContext with all DbSets
2. **Create Repositories**: Implement Repository<T> and specific repositories
3. **Run Migrations**: Create and apply EF Core migrations
4. **Add More Endpoints**: Implement wishlist, reviews, admin features
5. **Add Tests**: Create unit and integration tests
6. **Deploy**: Prepare for production deployment

---

## Common Patterns Used

### Service Dependency Injection
```csharp
public class MyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<MyService> _logger;

    public MyService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<MyService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
}
```

### Async/Await Pattern
```csharp
public async Task<ResultDto> GetDataAsync(Guid id)
{
    var entity = await _unitOfWork.Repository.GetByIdAsync(id);
    return _mapper.Map<ResultDto>(entity);
}
```

### Error Handling
```csharp
try
{
    // Operation
    return ApiResponse<T>.Ok(data, "Success message");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error message");
    return StatusCode(500, ApiResponse<T>.Error("User-friendly message"));
}
```

---

## Features by Service

### AuthService
- User registration with validation
- Login with JWT token generation
- Password hashing with BCrypt
- Email verification
- Password reset and change

### ProductService
- Product CRUD operations
- Search and filtering
- Pagination support
- Stock management
- Low stock alerts

### OrderService
- Order creation from cart
- Inventory deduction
- Order status management
- Promo code application
- Revenue reporting

### CartService
- User and guest cart support
- Add/remove/update items
- Cart validation
- Guest-to-user migration
- Promo code handling

---

For detailed information, see **IMPLEMENTATION_SUMMARY.md**
