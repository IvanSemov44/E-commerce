using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Users;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for user profile operations.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        _logger.LogInformation("Retrieving profile for user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

        return _mapper.Map<UserProfileDto>(user);
    }
}
