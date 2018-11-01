using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4.Test;
using System.Security.Claims;

namespace Latsic.IdServer
{
  public class Config
  {
    public static IEnumerable<ApiResource> GetApiResources()
    {
      return new List<ApiResource>
      {
          new ApiResource("api1", "My API"),

          new ApiResource
          {
            Name = "IdApi1",
            DisplayName = "A Test API",

            Scopes =
            {
              new Scope
              {
                Name = "IdApi1",
                DisplayName = "A Test API",
              }
            },

            UserClaims = new[] {JwtClaimTypes.BirthDate, JwtClaimTypes.Role, "UserNumber"}
          }
      };
    }

    public static IEnumerable<Client> GetClients()
    {
      return new List<Client>
      {
        new Client
        {
          ClientId = "client",

          // no interactive user, use the clientid/secret for authentication
          AllowedGrantTypes = GrantTypes.ClientCredentials,

          // secret for authentication
          ClientSecrets =
          {
              new Secret("secret".Sha256())
          },

          // scopes that client has access to
          AllowedScopes = { "api1" }
        },
        new Client
        {
            ClientId = "ro.client",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

            ClientSecrets =
            {
                new Secret("secret".Sha256())
            },
            AllowedScopes = { "api1" }
        },
        // OpenID Connect implicit flow client (MVC)
        new Client
        {
          ClientId = "mvc1",
          ClientName = "MVC Client",
          AllowedGrantTypes = GrantTypes.Implicit,

          // where to redirect to after login
          RedirectUris = { "http://localhost:7000/signin-oidc" },

          // where to redirect to after logout
          PostLogoutRedirectUris = { "http://localhost:7000/signout-callback-oidc" },

          AllowedScopes = new List<string>
          {
            IdentityServerConstants.StandardScopes.OpenId,
            IdentityServerConstants.StandardScopes.Profile
          }
        },
        new Client
        {
          ClientId = "js",
          ClientName = "JavaScript Client",
          AllowedGrantTypes = GrantTypes.Implicit,
          AllowAccessTokensViaBrowser = true,
          //AlwaysIncludeUserClaimsInIdToken = true,

          RedirectUris =           { "http://localhost:8080/callback.html", "http://localhost:8080/silent-renew.html" },
          PostLogoutRedirectUris = { "http://localhost:8080/index.html" },
          AllowedCorsOrigins =     { "http://localhost:8080" },

          IdentityTokenLifetime = 100, // seconds
          AccessTokenLifetime = 100, // seconds

          AllowedScopes =
          {
              IdentityServerConstants.StandardScopes.OpenId,
              IdentityServerConstants.StandardScopes.Profile,
              "IdApi1",
              "custom.profile"
          }
        }
      };
    }
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
      var customProfile = new IdentityResource(
        name: "custom.profile",
        displayName: "email, role and user number",
        claimTypes: new[] { JwtClaimTypes.Email, JwtClaimTypes.Role, "UserNumber" });

      return new List<IdentityResource>
      {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        customProfile
      };
    }
  }

  
}