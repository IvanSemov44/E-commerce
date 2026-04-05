using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Repositories;
using ECommerce.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
