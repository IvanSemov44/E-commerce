using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.SetDefaultAddress;

public class SetDefaultAddressCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IAddressProjectionEventPublisher addressProjectionEventPublisher
) : IRequestHandler<SetDefaultAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(SetDefaultAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var result = user.SetDefaultShippingAddress(command.AddressId);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        var selectedAddress = user.Addresses.FirstOrDefault(x => x.Id == command.AddressId);

        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        if (selectedAddress is not null)
        {
            await addressProjectionEventPublisher.PublishAddressProjectionUpdatedAsync(
                selectedAddress.Id,
                user.Id,
                selectedAddress.Street,
                selectedAddress.City,
                selectedAddress.Country,
                selectedAddress.PostalCode ?? string.Empty,
                false,
                ct);
        }

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
