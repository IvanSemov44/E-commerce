using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Users;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for user profile operations.
/// </summary>
public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserProfileDto>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving profile for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<UserProfileDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        return Result<UserProfileDto>.Ok(_mapper.Map<UserProfileDto>(user));
    }

    public async Task<Result<UserProfileDto>> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null)
            return Result<UserProfileDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

        return Result<UserProfileDto>.Ok(_mapper.Map<UserProfileDto>(user));
    }

    public async Task<Result<UserPreferencesDto>> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving preferences for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<UserPreferencesDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        // Return default preferences for now
        var preferences = new UserPreferencesDto
        {
            UserId = userId,
            EmailNotifications = true,
            SmsNotifications = false,
            PushNotifications = true,
            Language = "en",
            Currency = "USD",
            NewsletterSubscribed = false
        };

        return Result<UserPreferencesDto>.Ok(preferences);
    }

    public async Task<Result<UserPreferencesDto>> UpdateUserPreferencesAsync(Guid userId, UserPreferencesDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating preferences for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<UserPreferencesDto>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        // Return updated preferences
        var preferences = new UserPreferencesDto
        {
            UserId = userId,
            EmailNotifications = dto.EmailNotifications,
            SmsNotifications = dto.SmsNotifications,
            PushNotifications = dto.PushNotifications,
            Language = dto.Language,
            Currency = dto.Currency,
            NewsletterSubscribed = dto.NewsletterSubscribed
        };

        return Result<UserPreferencesDto>.Ok(preferences);
    }

    public async Task<Result<Unit>> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Changing password for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            return Result<Unit>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId}' not found");

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            return Result<Unit>.Fail(ErrorCodes.InvalidCredentials, "Current password is incorrect");

        var trackedUser = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: true, cancellationToken: cancellationToken);
        trackedUser!.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result<Unit>.Ok(new Unit());
    }
}

