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

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        if (await _unitOfWork.Users.EmailExistsAsync(dto.Email, cancellationToken: cancellationToken))
        {
            throw new DuplicateEmailException(dto.Email);
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

        return new AuthResponseDto
        {
            Success = true,
            Message = "Registration successful",
            User = userDto,
            Token = token
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken: cancellationToken);
        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash!))
        {
            throw new InvalidCredentialsException();
        }

        var userDto = _mapper.Map<UserDto>(user);
        var token = GenerateJwtToken(userDto);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful",
            User = userDto,
            Token = token
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // For MVP, simplified refresh token logic
        if (!await ValidateTokenAsync(token, cancellationToken))
        {
            throw new InvalidTokenException();
        }

        return new AuthResponseDto
        {
            Success = true,
            Message = "Token refreshed",
            Token = token
        };
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
                ValidateIssuer = false,
                ValidateAudience = false,
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

    public async Task VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        if (user.EmailVerificationToken != token)
        {
            throw new InvalidTokenException();
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken: cancellationToken);
        if (user == null)
        {
            // For security reasons, we don't throw an exception here
            // Instead, we return a dummy token that won't work
            // This prevents email enumeration attacks
            return Guid.NewGuid().ToString();
        }

        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

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

        return user.PasswordResetToken;
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken: cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException($"User with email {email} not found");
        }

        if (user.PasswordResetToken != token || user.PasswordResetExpires < DateTime.UtcNow)
        {
            throw new InvalidTokenException();
        }

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpires = null;
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        if (!VerifyPassword(oldPassword, user.PasswordHash!))
        {
            throw new InvalidCredentialsException();
        }

        user.PasswordHash = HashPassword(newPassword);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }

    
}
