using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using temp_clean_arch.Infrastructure.Identity;

namespace temp_clean_arch.Web.Endpoints;

[Authorize(Roles = "Admin,Administrator")] // Only admins can access
public class AdminUsers : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetUsers, "admin/users");
        groupBuilder.MapPost(AssignRole, "admin/users/{userId}/roles/{role}");
        groupBuilder.MapDelete(RemoveRole, "admin/users/{userId}/roles/{role}");
    }
    public static async Task<IResult> AssignRole(string userId, string role, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Results.NotFound("User not found");

        var result = await userManager.AddToRoleAsync(user, role);
        if (result.Succeeded)
            return Results.Ok();
        return Results.BadRequest(result.Errors);
    }

    public static async Task<IResult> RemoveRole(string userId, string role, UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Results.NotFound("User not found");

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (result.Succeeded)
            return Results.Ok();
        return Results.BadRequest(result.Errors);
    }

    public static async Task<IResult> GetUsers(UserManager<ApplicationUser> userManager)
    {
        var users = userManager.Users.ToList();
        var userList = new List<object>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userList.Add(new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                roles = roles
            });
        }
        return Results.Ok(userList);
    }
}
