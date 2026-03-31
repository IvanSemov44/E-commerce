using FluentValidation;

namespace ECommerce.Identity.Application.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}
