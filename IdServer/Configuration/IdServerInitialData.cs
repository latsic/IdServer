using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;
using IdentityServer4.Test;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Latsic.IdUserData.Models;

namespace Latsic.IdServer.Configuration
{
  public class IdServerInitialData
  {
    private readonly WebClients _webClients;
    private Dictionary<ApiResourceId, ApiResource> _apiResources;
    private Dictionary<IdentityResourceId, IdentityResource> _identityResources;

    public enum ApiResourceId {
      IdApi1,
      Api1,
      IdUserApi
    };

    public enum IdentityResourceId {
      OpenId,
      Profile,
      WebClientDefault
    };

    public IdServerInitialData(IOptions<WebClients> webClients)
    {
      _webClients = webClients.Value;
      _apiResources = getApiResources();
      _identityResources = getIdentityResources();
    }

    public Dictionary<ApiResourceId, ApiResource> ApiResources
    {
      get
      {
        return _apiResources;
      }
    }

    public Dictionary<IdentityResourceId, IdentityResource> IdentityResources
    {
      get
      {
        return _identityResources;
      }
    }

    public Dictionary<IdentityResourceId, IdentityResource> getIdentityResources()
    {
      return new Dictionary<IdentityResourceId, IdentityResource>
      {
        { IdentityResourceId.OpenId, new IdentityResources.OpenId() },
        { IdentityResourceId.Profile, new IdentityResources.Profile() },
        { IdentityResourceId.WebClientDefault, new IdentityResource
          (
            name: "Web_Client_Default",
            displayName: "email, role, user number and api access info",
            claimTypes: new[]
            {
              JwtClaimTypes.Email,
              JwtClaimTypes.Role,
              CustomClaims.UserNumber,
              CustomClaims.ApiAccess
            }
          )
        }
      };
    }

    private Dictionary<ApiResourceId, ApiResource> getApiResources()
    {
      return new Dictionary<ApiResourceId, ApiResource>
      {
        {
          ApiResourceId.Api1,
          new ApiResource("api1", "My API")
        },
        {
          ApiResourceId.IdUserApi,
          new ApiResource
          {
            Name = "IdUserApi",
            DisplayName = "Manage Users",

            // To declare sub-scopes. Add blocks like the following.
            // Here there is no sub-scope. The name is therefore identical to
            // the main scope.
            Scopes =
            {
              new Scope{ Name = "IdUserApi", DisplayName = "Manage Users" }
            },

            // These Claims will be inside the issued accesstokens.
            // The API can then use these claims for claims based
            // authorization.
            UserClaims = new[]
            {
              JwtClaimTypes.Role,
              CustomClaims.ApiAccess
            }
          }
        },
        {
          ApiResourceId.IdApi1,
          new ApiResource
          {
            Name = "IdApi1",
            DisplayName = "An Api to test and learn authorization",

            // To declare sub-scopes. Add blocks like the following.
            // Here there is no sub-scope. The name is therefore identical to
            // the main scope.
            Scopes =
            {
              new Scope{ Name = "IdApi1", DisplayName = "An Api to test and learn authorization" }
            },

            // These Claims will be inside the issued accesstokens.
            // The API can then use these claims for claims based
            // authorization.
            UserClaims = new[]
            {
              JwtClaimTypes.BirthDate,
              JwtClaimTypes.Role,
              CustomClaims.UserNumber,
              CustomClaims.ApiAccess
            }
          }
        }
      };
    }

    public IEnumerable<Client> GetClients()
    {
      var clients = new List<Client>
      {
        // new Client
        // {
        //   ClientId = "mvc1",
        //   ClientName = "MVC Client",
        //   AllowedGrantTypes = GrantTypes.Implicit,

        //   // where to redirect to after login
        //   RedirectUris = { "http://localhost:7000/signin-oidc" },

        //   // where to redirect to after logout
        //   PostLogoutRedirectUris = { "http://localhost:7000/signout-callback-oidc" },

        //   AllowedScopes = new List<string>
        //   {
        //     IdentityServerConstants.StandardScopes.OpenId,
        //     IdentityServerConstants.StandardScopes.Profile
        //   }
        // }
        // new Client
        // {
        //   ClientId = "client",

        //   // no interactive user, use the clientid/secret for authentication
        //   AllowedGrantTypes = GrantTypes.ClientCredentials,

        //   // secret for authentication
        //   ClientSecrets =
        //   {
        //     new Secret("secret".Sha256())
        //   },

        //   // scopes that client has access to
        //   AllowedScopes = { ApiResources[ApiResourceId.Api1].Name }
        // },
        // new Client
        // {
        //   ClientId = "ro.client",
        //   AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

        //   ClientSecrets =
        //   {
        //     new Secret("secret".Sha256())
        //   },
        //   AllowedScopes = { ApiResources[ApiResourceId.Api1].Name }
        // }
      };

      foreach(var webClient in _webClients.Clients)
      {
        clients.Add(new Client
        {
          ClientId = webClient.ClientId,
          ClientName = "Web Client",
          AllowedGrantTypes = GrantTypes.Implicit,
          AllowAccessTokensViaBrowser = true,
          //AlwaysIncludeUserClaimsInIdToken = true,
          
          PostLogoutRedirectUris = { $"{webClient.Uri}/index.html" },
          AllowedCorsOrigins =     { $"{webClient.Uri}" },
          RedirectUris =
          { 
            $"{webClient.Uri}/callback.html",
            $"{webClient.Uri}/silent-renew.html",
            $"{webClient.Uri}/popup-callback.html"
          },

          IdentityTokenLifetime = (int)webClient.IdentityTokenLifetimeSeconds,
          AccessTokenLifetime = (int)webClient.AccessTokenLifetimeSeconds,

          AllowedScopes =
          {
            IdentityResources[IdentityResourceId.OpenId].Name,
            IdentityResources[IdentityResourceId.Profile].Name,
            IdentityResources[IdentityResourceId.WebClientDefault].Name,
            ApiResources[ApiResourceId.IdApi1].Name,
            ApiResources[ApiResourceId.IdUserApi].Name
          }
        });
      }

      return clients;
    }
  }
}