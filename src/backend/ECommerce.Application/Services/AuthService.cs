using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Application.DTOs.Auth;
using AutoMapper;
using ECommerce.Application.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Threading;

namespace ECommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService, IMapper mapper, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        if (await _unitOfWork.Users.EmailExistsAsync(dto.Email, cancellationToken: cancellationToken))
        {
            return Result<AuthResponseDto>.Fail("DUPLICATE_EMAIL", $"Email '{dto.Email}' is already registered");
        }

        var user = new User
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = HashPassword(dto.Password),
            Role = UserRole.Customer,
            IsEmailVerified = true, // For MVP, auto-verify
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("New user registered: {Email} (ID: {UserId})", user.Email, user.Id);

        // Send welcome email (fire and forget - don't block registration)
        var verificationLink = $"{_configuration["AppUrl"]}/verify-email?userId={user.Id}&token={user.EmailVerificationToken}";
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, verificationLink);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
            }
        });

        var userDto = _mapper.Map<UserDto>(user);
        var token = GenerateJwtToken(userDto);
        var refreshToken = GenerateRefreshToken();

        // Save refresh token
        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered: {Email} (ID: {UserId})", user.Email, user.Id);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful",
            User = userDto,
            Token = token,
            RefreshToken = refreshToken
        });
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken: cancellationToken);
        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash!))
        {
            _logger.LogWarning("Failed login attempt for {Email}: Invalid credentials", dto.Email);
            return Result<AuthResponseDto>.Fail("INVALID_CREDENTIALS", "Email or password is incorrect");
        }

        var userDto = _mapper.Map<UserDto>(user);
        var token = GenerateJwtToken(userDto);
        var refreshToken = GenerateRefreshToken();

        // Save refresh token
        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successful login for user {Email} (ID: {UserId})", user.Email, user.Id);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            User = userDto,
            Token = token,
            RefreshToken = refreshToken
        });
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var storedToken = await _unitOfWork.RefreshTokens
            .FindByCondition(rt => rt.Token == token && !rt.IsRevoked)
            .FirstOrDefaultAsync(cancellationToken);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponseDto>.Fail("INVALID_TOKEN", "The refresh token is invalid or expired");
        }

        // Revoke old refresh token (rotation)
        storedToken.IsRevoked = true;

        // Get user and generate new tokens
        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
        {
            return Result<AuthResponseDto>.Fail("USER_NOT_FOUND", "The user associated with this token no longer exists");
        }

        var userDto = _mapper.Map<UserDto>(user);
        var newAccessToken = GenerateJwtToken(userDto);
        var newRefreshToken = GenerateRefreshToken();

        // Save new refresh token
        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed",
            User = userDto,
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateJwtToken(UserDto user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("name", $"{user.FirstName} {user.LastName}"),
                new Claim("role", user.Role)
            },
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public async Task<Result<Unit>> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null)
        {
            return Result<Unit>.Fail("USER_NOT_FOUND", "User not found");
        }

        if (user.EmailVerificationToken != token)
        {
            return Result<Unit>.Fail("INVALID_TOKEN", "The email verification token is invalid or expired");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Email verified for user {UserId}", userId);

        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken: cancellationToken);
        if (user == null)
        {
            // For security reasons, we don't fail explicitly here
            // Instead, we return a dummy token that won't work
            // This prevents email enumeration attacks
            _logger.LogWarning("Password reset requested for non-existent email {Email}", email);
            return Result<string>.Ok(Guid.NewGuid().ToString());
        }

        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Password reset token generated for {Email}", email);

        // Send password reset email (fire and forget)
        var resetLink = $"{_configuration["AppUrl"]}/reset-password?email={email}&token={user.PasswordResetToken}";
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        });

        return Result<string>.Ok(user.PasswordResetToken);
    }

    public async Task<Result<Unit>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken: cancellationToken);
        if (user == null)
        {
            return Result<Unit>.Fail("USER_NOT_FOUND", "User not found");
        }

        if (user.PasswordResetToken != token || user.PasswordResetExpires < DateTime.UtcNow)
        {
            return Result<Unit>.Fail("INVALID_TOKEN", "The password reset token is invalid or expired");
        }

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpires = null;
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Password reset for user {Email}", email);

        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null)
        {
            return Result<Unit>.Fail("USER_NOT_FOUND", "User not found");
        }

        if (!VerifyPassword(oldPassword, user.PasswordHash!))
        {
            return Result<Unit>.Fail("INVALID_CREDENTIALS", "The current password is incorrect");
        }

        user.PasswordHash = HashPassword(newPassword);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}", userId);

        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        if (user == null)
        {
            return null;
        }
        return _mapper.Map<UserDto>(user);
    }

    
}
