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
        // Add endpoints for role management as needed
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
