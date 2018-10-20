using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4.Test;
using System.Security.Claims;

namespace Latsic.IdServer
{
  public class Config
  {
    // public static List<TestUser> GetUsers()
    // {
    //   return new List<TestUser>
    //   {
    //     new TestUser
    //     {
    //         SubjectId = "1",
    //         Username = "alice",
    //         Password = "password",
    //         Claims = new List<Claim>
    //         {
    //           new Claim("name", "Alice"),
    //           new Claim("website", "https://alice.com")
    //         }
    //     },
    //     new TestUser
    //     {
    //         SubjectId = "2",
    //         Username = "bob",
    //         Password = "password",
    //         Claims = new List<Claim>
    //         {
    //           new Claim("name", "Bob"),
    //           new Claim("website", "https://bob.com")
    //         }
    //     }
    //   };
    // }

    public static IEnumerable<ApiResource> GetApiResources()
    {
      return new List<ApiResource>
      {
          new ApiResource("api1", "My API")
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

          RedirectUris =           { "http://localhost:8003/callback.html" },
          PostLogoutRedirectUris = { "http://localhost:8003/index.html" },
          AllowedCorsOrigins =     { "http://localhost:8003" },

          AllowedScopes =
          {
              IdentityServerConstants.StandardScopes.OpenId,
              IdentityServerConstants.StandardScopes.Profile,
              "api1"
          }
        }
      };
    }
    public static IEnumerable<IdentityResource> GetIdentityResources()
    {
      return new List<IdentityResource>
      {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
      };
    }
  }

  
}