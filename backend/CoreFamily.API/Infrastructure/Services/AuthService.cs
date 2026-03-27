using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly CoreFamilyDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly IConfiguration _config;

    public AuthService(
        CoreFamilyDbContext db,
        IPasswordService passwords,
        ITokenService tokens,
        IConfiguration config)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // 1. Check duplicate email
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLowerInvariant()))
            throw new InvalidOperationException("An account with this email already exists.");

        // 2. Parse role & category
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
            throw new ArgumentException($"Invalid role: {dto.Role}");
        if (!Enum.TryParse<UserCategory>(dto.Category, true, out var category))
            throw new ArgumentException($"Invalid category: {dto.Category}");

        // 3. Create user
        var user = new User
        {
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = _passwords.Hash(dto.Password),
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        var userRole = new UserRole_ { UserId = user.Id, Role = role, User = user };
        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Category = category,
            User = user
        };

        _db.Users.Add(user);
        _db.UserRoles.Add(userRole);
        _db.UserProfiles.Add(profile);

        // 4. Refresh token
        var refreshToken = await CreateRefreshTokenAsync(user.Id, null);
        await _db.SaveChangesAsync();

        // TODO: Send verification email via IEmailService

        return BuildAuthResponse(user, profile, [role.ToString()], refreshToken.Token);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is suspended. Contact support.");

        if (!_passwords.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        user.LastLoginAt = DateTime.UtcNow;

        // Revoke any old refresh tokens for this user
        var oldTokens = await _db.Set<RefreshToken>()
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();
        foreach (var t in oldTokens) t.IsRevoked = true;

        var refreshToken = await CreateRefreshTokenAsync(user.Id, null);
        await _db.SaveChangesAsync();

        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();
        return BuildAuthResponse(user, user.Profile!, roles, refreshToken.Token);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var token = await _db.Set<RefreshToken>()
            .Include(rt => rt.User)
            .ThenInclude(u => u.Profile)
            .Include(rt => rt.User)
            .ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

        token.IsRevoked = true;
        var newRefreshToken = await CreateRefreshTokenAsync(token.UserId, null);
        await _db.SaveChangesAsync();

        var roles = token.User.Roles.Select(r => r.Role.ToString()).ToList();
        return BuildAuthResponse(token.User, token.User.Profile!, roles, newRefreshToken.Token);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _db.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (token is { IsActive: true })
        {
            token.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user is null) return; // Silent — never reveal if email exists

        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        // TODO: Send password reset email via IEmailService
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == dto.Token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow)
            ?? throw new InvalidOperationException("Reset token is invalid or has expired.");

        user.PasswordHash = _passwords.Hash(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _db.SaveChangesAsync();
    }

    public async Task VerifyEmailAsync(string token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.EmailVerificationToken == token &&
            u.EmailVerificationTokenExpiry > DateTime.UtcNow)
            ?? throw new InvalidOperationException("Verification token is invalid or has expired.");

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        await _db.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────────────
    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string? ip)
    {
        var days = int.Parse(_config["Jwt:RefreshExpiryDays"] ?? "7");
        var token = new RefreshToken
        {
            UserId = userId,
            Token = _tokens.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            CreatedByIp = ip
        };
        _db.Set<RefreshToken>().Add(token);
        return await Task.FromResult(token);
    }

    private AuthResponseDto BuildAuthResponse(
        User user, UserProfile profile,
        IEnumerable<string> roles, string refreshToken)
    {
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        var accessToken = _tokens.GenerateAccessToken(user.Id, user.Email, roles);
        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes),
            User: new UserSummaryDto(
                user.Id,
                user.Email,
                profile.FirstName,
                profile.LastName,
                roles.FirstOrDefault() ?? "Client",
                profile.Category.ToString(),
                profile.AvatarUrl)
        );
    }
}
