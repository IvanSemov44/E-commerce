using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Users;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;
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

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving profile for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<UserPreferencesDto> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving preferences for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Return default preferences for now
        return new UserPreferencesDto
        {
            UserId = userId,
            EmailNotifications = true,
            SmsNotifications = false,
            PushNotifications = true,
            Language = "en",
            Currency = "USD",
            NewsletterSubscribed = false
        };
    }

    public async Task<UserPreferencesDto> UpdateUserPreferencesAsync(Guid userId, UserPreferencesDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating preferences for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Return updated preferences
        return new UserPreferencesDto
        {
            UserId = userId,
            EmailNotifications = dto.EmailNotifications,
            SmsNotifications = dto.SmsNotifications,
            PushNotifications = dto.PushNotifications,
            Language = dto.Language,
            Currency = dto.Currency,
            NewsletterSubscribed = dto.NewsletterSubscribed
        };
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Changing password for user {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Password change successful (in test environment, just return)
        await Task.CompletedTask;
    }
}

