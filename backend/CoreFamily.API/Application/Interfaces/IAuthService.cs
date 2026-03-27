using CoreFamily.API.Application.DTOs;

namespace CoreFamily.API.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task VerifyEmailAsync(string token);
}

public interface IUserService
{
    Task<UserSummaryDto?> GetByIdAsync(Guid userId);
    Task<UserSummaryDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string refreshToken);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
