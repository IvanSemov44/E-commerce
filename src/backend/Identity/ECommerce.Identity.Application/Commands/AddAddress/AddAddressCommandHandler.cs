using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.AddAddress;

public class AddAddressCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IAddressProjectionEventPublisher addressProjectionEventPublisher
) : IRequestHandler<AddAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(AddAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var existingAddressIds = user.Addresses.Select(x => x.Id).ToHashSet();

        var result = user.AddAddress(command.Street, command.City, command.Country, command.PostalCode);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        var addedAddress = user.Addresses.FirstOrDefault(x => !existingAddressIds.Contains(x.Id));
        if (addedAddress is not null)
        {
            await addressProjectionEventPublisher.PublishAddressProjectionUpdatedAsync(
                addedAddress.Id,
                user.Id,
                addedAddress.Street,
                addedAddress.City,
                addedAddress.Country,
                addedAddress.PostalCode ?? string.Empty,
                false,
                ct);
        }

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
