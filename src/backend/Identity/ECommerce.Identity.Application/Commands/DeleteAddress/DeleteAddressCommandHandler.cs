using ECommerce.Identity.Application.DTOs;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Application.Extensions;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.DeleteAddress;

public class DeleteAddressCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IAddressProjectionEventPublisher addressProjectionEventPublisher)
    : IRequestHandler<DeleteAddressCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(DeleteAddressCommand command, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(command.UserId, ct);
        if (user is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.UserNotFound);

        var deletedAddress = user.Addresses.FirstOrDefault(x => x.Id == command.AddressId);
        if (deletedAddress is null) return Result<UserProfileDto>.Fail(IdentityApplicationErrors.AddressNotFound);

        var result = user.DeleteAddress(command.AddressId);
        if (!result.IsSuccess) return Result<UserProfileDto>.Fail(result.GetErrorOrThrow());

        await users.UpdateAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        await addressProjectionEventPublisher.PublishAddressProjectionUpdatedAsync(
            deletedAddress.Id,
            user.Id,
            deletedAddress.Street,
            deletedAddress.City,
            deletedAddress.Country,
            deletedAddress.PostalCode ?? string.Empty,
            true,
            ct);

        return Result<UserProfileDto>.Ok(user.ToProfileDto());
    }
}
