using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ECommerce.Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(DependencyInjection).Assembly;

        foreach (Type implementationType in applicationAssembly.GetTypes())
        {
            if (implementationType.IsAbstract || implementationType.IsInterface)
                continue;

            Type[] validatorInterfaces = implementationType
                .GetInterfaces()
                .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                .ToArray();

            foreach (Type validatorInterface in validatorInterfaces)
                services.AddTransient(validatorInterface, implementationType);
        }

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}