using FluentValidation;
using ECommerce.Reviews.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReviewsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateReviewRequestDtoValidator>();
        return services;
    }
}
