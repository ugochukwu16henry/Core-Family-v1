using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace temp_clean_arch.Web;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources => new[]
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
    };

    public static IEnumerable<ApiScope> ApiScopes => new[]
    {
        new ApiScope("api1", "Core Family API")
    };

    public static IEnumerable<Client> Clients => new[]
    {
        new Client
        {
            ClientId = "angular_spa",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RedirectUris = { "https://localhost:4200/auth-callback" },
            AllowedScopes = { "openid", "profile", "api1" },
            AllowAccessTokensViaBrowser = true,
            RequirePkce = true,
            AllowedCorsOrigins = { "https://localhost:4200" }
        },
        new Client
        {
            ClientId = "blazor_client",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RedirectUris = { "https://localhost:5002/authentication/login-callback" },
            PostLogoutRedirectUris = { "https://localhost:5002/" },
            AllowedScopes = { "openid", "profile", "api1" },
            RequirePkce = true,
            AllowedCorsOrigins = { "https://localhost:5002" }
        }
    };
}
