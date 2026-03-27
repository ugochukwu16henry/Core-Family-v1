using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly CoreFamilyDbContext _db;

    public UserService(CoreFamilyDbContext db) => _db = db;

    public async Task<UserSummaryDto?> GetByIdAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Profile is null) return null;

        return new UserSummaryDto(
            user.Id,
            user.Email,
            user.Profile.FirstName,
            user.Profile.LastName,
            user.Roles.FirstOrDefault()?.Role.ToString() ?? "Client",
            user.Profile.Category.ToString(),
            user.Profile.AvatarUrl);
    }

    public async Task<UserSummaryDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        var profile = user.Profile!;
        if (dto.FirstName is not null) profile.FirstName = dto.FirstName.Trim();
        if (dto.LastName is not null) profile.LastName = dto.LastName.Trim();
        if (dto.Bio is not null) profile.Bio = dto.Bio;
        if (dto.Country is not null) profile.Country = dto.Country;
        if (dto.City is not null) profile.City = dto.City;
        if (dto.PhoneNumber is not null) profile.PhoneNumber = dto.PhoneNumber;
        if (dto.PreferredLanguage is not null) profile.PreferredLanguage = dto.PreferredLanguage;
        if (dto.TimeZone is not null) profile.TimeZone = dto.TimeZone;

        await _db.SaveChangesAsync();

        return new UserSummaryDto(
            user.Id, user.Email,
            profile.FirstName, profile.LastName,
            user.Roles.FirstOrDefault()?.Role.ToString() ?? "Client",
            profile.Category.ToString(),
            profile.AvatarUrl);
    }
}
