using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;

using Latsic.IdServer.Models;
using Latsic.IdServer.Data;

namespace Host.Quickstart.Account
{
  [SecurityHeaders]
  [AllowAnonymous]
  public class ExternalController : Controller
  {
    //private readonly TestUserStore _users;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly IEventService _events;
    private readonly UserManager<IdUser> _userManager;
    private readonly SignInManager<IdUser> _signInManager;
    private readonly IdUserDbContext _idUserDbContext;
    private string GoogleClaimTypeEmail
    {
      get
      {
        return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
      }
    }

    public ExternalController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IEventService events,
            UserManager<IdUser> userManager,
            SignInManager<IdUser> signInManager,
            IdUserDbContext idUserDbContext)
    {
      // if the TestUserStore is not in DI, then we'll just use the global users collection
      // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)
      //_users = users ?? new TestUserStore(TestUsers.Users);
      _userManager = userManager;
      _signInManager = signInManager;
      _idUserDbContext = idUserDbContext;
      _interaction = interaction;
      _clientStore = clientStore;
      _events = events;
    }

    /// <summary>
    /// initiate roundtrip to external authentication provider
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Challenge(string provider, string returnUrl)
    {
      if (string.IsNullOrEmpty(returnUrl)) returnUrl = "~/";

      // validate returnUrl - either it is a valid OIDC URL or back to a local page
      if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false)
      {
        // user might have clicked on a malicious link - should be logged
        throw new Exception("invalid return URL");
      }

      if (AccountOptions.WindowsAuthenticationSchemeName == provider)
      {
        // windows authentication needs special handling
        return await ProcessWindowsLoginAsync(returnUrl);
      }
      else
      {
        // start challenge and roundtrip the return URL and scheme 
        var props = new AuthenticationProperties
        {
          RedirectUri = Url.Action(nameof(Callback)),
          Items =
                    {
                        { "returnUrl", returnUrl },
                        { "scheme", provider },
                    }
        };

        return Challenge(props, provider);
      }
    }

    /// <summary>
    /// Post processing of external authentication
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Callback()
    {
      // read external identity from the temporary cookie
      var result = await HttpContext.AuthenticateAsync(IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme);
      if (result?.Succeeded != true)
      {
        throw new Exception("External authentication error");
      }

      // lookup our user and external provider info
      var (user, provider, providerUserId, claims) = await FindUserFromExternalProvider(result);

      user = await IntegrateExternalUser(provider, providerUserId, claims, user);

      // if (user == null)
      // {
      //     // this might be where you might initiate a custom workflow for user registration
      //     // in this sample we don't show how that would be done, as our sample implementation
      //     // simply auto-provisions new external user
      //     user = await IntegrateExternalUser(provider, providerUserId, claims);
      // }

      // this allows us to collect any additonal claims or properties
      // for the specific prtotocols used and store them in the local auth cookie.
      // this is typically used to store data needed for signout from those protocols.
      var additionalLocalClaims = new List<Claim>();
      var localSignInProps = new AuthenticationProperties();
      ProcessLoginCallbackForOidc(result, additionalLocalClaims, localSignInProps);
      ProcessLoginCallbackForWsFed(result, additionalLocalClaims, localSignInProps);
      ProcessLoginCallbackForSaml2p(result, additionalLocalClaims, localSignInProps);

      // issue authentication cookie for user
      await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.Id, user.UserName));
      //await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.SubjectId, user.Username));
      await HttpContext.SignInAsync(user.Id, user.UserName, provider, localSignInProps, additionalLocalClaims.ToArray());
      //await HttpContext.SignInAsync(user.SubjectId, user.Username, provider, localSignInProps, additionalLocalClaims.ToArray());

      // delete temporary cookie used during external authentication
      await HttpContext.SignOutAsync(IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme);

      // retrieve return URL
      var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

      // check if external login is in the context of an OIDC request
      var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
      if (context != null)
      {
        if (await _clientStore.IsPkceClientAsync(context.ClientId))
        {
          // if the client is PKCE then we assume it's native, so this change in how to
          // return the response is for better UX for the end user.
          return View("Redirect", new RedirectViewModel { RedirectUrl = returnUrl });
        }
      }

      return Redirect(returnUrl);
    }

    private async Task<IActionResult> ProcessWindowsLoginAsync(string returnUrl)
    {
      // see if windows auth has already been requested and succeeded
      var result = await HttpContext.AuthenticateAsync(AccountOptions.WindowsAuthenticationSchemeName);
      if (result?.Principal is WindowsPrincipal wp)
      {
        // we will issue the external cookie and then redirect the
        // user back to the external callback, in essence, treating windows
        // auth the same as any other external authentication mechanism
        var props = new AuthenticationProperties()
        {
          RedirectUri = Url.Action("Callback"),
          Items =
                    {
                        { "returnUrl", returnUrl },
                        { "scheme", AccountOptions.WindowsAuthenticationSchemeName },
                    }
        };

        var id = new ClaimsIdentity(AccountOptions.WindowsAuthenticationSchemeName);
        id.AddClaim(new Claim(JwtClaimTypes.Subject, wp.Identity.Name));
        id.AddClaim(new Claim(JwtClaimTypes.Name, wp.Identity.Name));

        // add the groups as claims -- be careful if the number of groups is too large
        if (AccountOptions.IncludeWindowsGroups)
        {
          var wi = wp.Identity as WindowsIdentity;
          var groups = wi.Groups.Translate(typeof(NTAccount));
          var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));
          id.AddClaims(roles);
        }

        await HttpContext.SignInAsync(
            IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme,
            new ClaimsPrincipal(id),
            props);
        return Redirect(props.RedirectUri);
      }
      else
      {
        // trigger windows auth
        // since windows auth don't support the redirect uri,
        // this URL is re-triggered when we call challenge
        return Challenge(AccountOptions.WindowsAuthenticationSchemeName);
      }
    }

    private async Task<(IdUser user, string provider, string providerUserId, IEnumerable<Claim> claims)>
    FindUserFromExternalProvider(AuthenticateResult result)
    {
      var externalUser = result.Principal;

      // try to determine the unique id of the external user (issued by the provider)
      // the most common claim type for that are the sub claim and the NameIdentifier
      // depending on the external provider, some other claim type might be used
      var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                        externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                        throw new Exception("Unknown userid");

      // remove the user id claim so we don't include it as an extra claim if/when we provision the user
      var claims = externalUser.Claims.ToList();
      claims.Remove(userIdClaim);

      var provider = result.Properties.Items["scheme"];
      var providerUserId = userIdClaim.Value;

      // find external user

      var user = await _userManager.FindByLoginAsync(provider, providerUserId);

      //var user = _users.FindByExternalProvider(provider, providerUserId);

      return (user, provider, providerUserId, claims);
    }

    private async Task<IdUser> IntegrateExternalUser(string provider, string providerUserId, IEnumerable<Claim> claims, IdUser user = null)
    {
      if (user == null)
      {
        user = new IdUser();
        _idUserDbContext.Add(user);
        await _userManager.UpdateSecurityStampAsync(user);
      }


      var existingUserClaims = await _userManager.GetClaimsAsync(user);
      var claimsToRemove = existingUserClaims.Where(claim => claim.Issuer == provider);
      await _userManager.RemoveClaimsAsync(user, claimsToRemove);

      var claimsToUse = new List<Claim>();

      foreach (var claim in claims)
      {
        if (claim.Type == ClaimTypes.Name)
        {
          //claimsToUse.Add(new Claim(JwtClaimTypes.Name, claim.Value));
          claimsToUse.Add(new Claim(JwtClaimTypes.Name, claim.Value, claim.ValueType, provider, claim.OriginalIssuer, claim.Subject));

        }
        else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.ContainsKey(claim.Type))
        {
          //claimsToUse.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value));
          claimsToUse.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value, claim.ValueType, provider, claim.OriginalIssuer, claim.Subject));
        }
        else
        {
          // if(claim.Type == ClaimTypes.Email || claim.Type == GoogleClaimTypeEmail)
          // {
          //     user.Email = claim.Value;
          //     user.NormalizedEmail = claim.Value;
          // }
          // if(claim.Type == ClaimTypes.MobilePhone)
          // {
          //     user.PhoneNumber = claim.Value;
          // }

          claimsToUse.Add(claim);
        }
      }

      // if no display name was provided, try to construct by first and/or last name
      if (!claimsToUse.Any(claim => claim.Type == JwtClaimTypes.Name))
      {
        var first = claimsToUse.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
        var last = claimsToUse.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
        if (first != null && last != null)
        {
          //claimsToUse.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
          claimsToUse.Add(new Claim(JwtClaimTypes.Name, first + " " + last, null, provider));
        }
        else if (first != null)
        {
          //claimsToUse.Add(new Claim(JwtClaimTypes.Name, first));
          claimsToUse.Add(new Claim(JwtClaimTypes.Name, first, null, provider));
        }
        else if (last != null)
        {
          //claimsToUse.Add(new Claim(JwtClaimTypes.Name, last));
          claimsToUse.Add(new Claim(JwtClaimTypes.Name, last, null, provider));
        }
      }

      user.UserName = claimsToUse.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? user.Id;
      user.NormalizedUserName = user.UserName;
      // await _userManager.UpdateSecurityStampAsync(user);
      await _userManager.AddClaimsAsync(user, claims);


      var externalLogins = await _userManager.GetLoginsAsync(user);
      if (externalLogins == null || externalLogins.Count == 0)
      {
        await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerUserId, user.UserName));
      }

      await _idUserDbContext.SaveChangesAsync();

      return user;
    }

    private void ProcessLoginCallbackForOidc(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
    {
      // if the external system sent a session id claim, copy it over
      // so we can use it for single sign-out
      var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
      if (sid != null)
      {
        localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
      }

      // if the external provider issued an id_token, we'll keep it for signout
      var id_token = externalResult.Properties.GetTokenValue("id_token");
      if (id_token != null)
      {
        localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
      }
    }

    private void ProcessLoginCallbackForWsFed(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
    {
    }

    private void ProcessLoginCallbackForSaml2p(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
    {
    }
  }
}
