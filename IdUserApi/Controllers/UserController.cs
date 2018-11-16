using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

using Latsic.IdUserApi.Models.TransferObjects;
using Latsic.IdUserData.Models;
using Latsic.IdUserData.DataContexts;

namespace Latsic.IdUserApi.Controllers
{
  [Route("[controller]/[action]")]
  [ApiController]
  [EnableCors("AllowAllOrigins")]
  [Authorize(Policy = "ApiAccess")]
  public class UserController : ControllerBase
  {
    private readonly UserManager<IdUser> _userManager;
    private readonly IdUserDbContext _identityDbContext;
    private readonly ILogger<UserController> _logger;

    public UserController(
      UserManager<IdUser> userManager,
      IdUserDbContext identityDbContext,
      ILogger<UserController> logger)
    {
      _userManager = userManager;
      _identityDbContext = identityDbContext;
      _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<UserBasicDto>>> Users()
    {
      var userData = _userManager.Users.Select(user =>
        new UserBasicDto
        {
          UserId = user.Id,
          UserName = user.UserName
        });

      var a = await _userManager.Users.Select(user =>
        new UserBasicDto
        {
          UserId = user.Id,
          UserName = user.UserName
        }).ToListAsync();

      return Ok(await _userManager.Users.Select(user =>
        new UserBasicDto
        {
          UserId = user.Id,
          UserName = user.UserName
        }).ToListAsync());
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> Claims([FromRoute] string userId)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return notFoundUser(userId);

      var claims = _identityDbContext.UserClaims
        .Where(claim => claim.UserId == userId)
        .Select(claim =>
          new ClaimDto
          {
            Id = claim.Id.ToString(),
            Type = claim.ClaimType,
            Value = claim.ClaimValue
          });
      return Ok(await claims.ToListAsync());
    }

    [HttpDelete("{userId}/{claimType}/{claimValue}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ActionName("Claim")]
    public async Task<ActionResult<ClaimDto>> ClaimDelete(
      [FromRoute] string userId, [FromRoute] string claimType, string claimValue)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return notFoundUser(userId);

      var userClaimsToDelete = (await _userManager.GetClaimsAsync(user)).Where(claim =>
        claim.Type == claimType && (claim.Value == claimValue || string.IsNullOrEmpty(claimValue)));

      if (userClaimsToDelete.Count() == 0)
      {
        return notFoundClaim(userId, claimType, claimValue);
      }

     
      await _userManager.RemoveClaimsAsync(user, userClaimsToDelete);

      var deletedClaim = userClaimsToDelete.First();
      return Ok(new ClaimDto
      {
        Type = deletedClaim.Type,
        Value = deletedClaim.Value
      });
    }

    [HttpPut("{userId}/{claimId}/{claimType}/{claimValue}/{claimIssuer?}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ActionName("Claim")]
    public async Task<ActionResult<ClaimDto>> ClaimPut(
      [FromRoute] string userId, [FromRoute] string claimId,
      [FromRoute] string claimType, [FromRoute] string claimValue,
      [FromRoute] string claimIssuer = null)
    {
      if (string.IsNullOrWhiteSpace(claimType)) return badRequestClaimType(claimType);

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return notFoundUser(userId);

      var claimToUpdate = await _identityDbContext.UserClaims.FirstOrDefaultAsync(claim => claim.Id.ToString() == claimId);
      if (claimToUpdate == null)
      {
        return notFoundClaim(userId, claimId);
      }

      claimToUpdate.ClaimType = claimType;
      claimToUpdate.ClaimValue = claimValue;
      claimToUpdate.Issuer = claimIssuer ?? "";

      if (await _identityDbContext.SaveChangesAsync() > 0)
      {
        return Ok(new ClaimDto
        {
          Id = claimId,
          Type = claimType,
          Value = claimValue
        });
      }

      return databaseError();
    }

    [HttpPost("{userId}/{claimType}/{claimValue}/{claimIssuer?}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ActionName("Claim")]
    public async Task<ActionResult<ClaimDto>> ClaimPost(
      [FromRoute] string userId, [FromRoute] string claimType,
      [FromRoute] string claimValue, [FromRoute] string claimIssuer = null)
    {
      if (string.IsNullOrWhiteSpace(claimType)) return badRequestClaimType(claimType);

      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return notFoundUser(userId);

      var existingClaim = await _identityDbContext.UserClaims.FirstOrDefaultAsync(claim =>
        claim.UserId == userId && claim.ClaimType == claimType && claim.ClaimValue == claimValue);

      if (existingClaim != null)
      {
        return Ok(new ClaimDto { Id = existingClaim.Id.ToString(), Type = claimType, Value = claimValue });
      }

      IdUserClaim newClaim = new IdUserClaim
      {
        ClaimType = claimType,
        ClaimValue = claimValue,
        UserId = userId,
        Issuer = claimIssuer ?? ""
      };

      _identityDbContext.Add(newClaim);

      if (await _identityDbContext.SaveChangesAsync() > 0)
      {
        return Ok(new ClaimDto
        {
          Id = newClaim.Id.ToString(),
          Type = newClaim.ClaimType,
          Value = newClaim.ClaimValue
        });
      }

      return databaseError();
    }

    [HttpGet("{userId}/{claimType}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> Claim(
      [FromRoute] string userId, [FromRoute] string claimType)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return notFoundUser(userId);

      var userClaimsOfType = (await _userManager.GetClaimsAsync(user)).Where(claim => claim.Type == claimType);

      return Ok(userClaimsOfType.Select(claim =>
        new ClaimDto
        {
          Type = claim.Type,
          Value = claim.Value
        }));
    }

    private NotFoundObjectResult notFoundUser(string userId)
    {
      return NotFound(new
      {
        message = $"No user with id {userId}"
      });
    }

    private NotFoundObjectResult notFoundClaim(string userId, string claimType, string claimValue)
    {
      return NotFound(new
      {
        message = $"No Claim '{claimType}' with value '${claimValue}' found for user with id ${userId}"
      });
    }

    private NotFoundObjectResult notFoundClaim(string userId, string claimId)
    {
      return NotFound(new
      {
        message = $"No Claim with id '{claimId}' found for user with id ${userId}"
      });
    }

    private BadRequestObjectResult badRequestClaimType(string claimType)
    {
      return BadRequest(new
      {
        message = $"Invalid claim type '{claimType}'"
      });
    }

    private ObjectResult databaseError()
    {
      return StatusCode(500, new
      {
        message = "Database error"
      });
    }
  }
}



