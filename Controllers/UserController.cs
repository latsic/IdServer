using System;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using IdentityModel;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;

using Latsic.IdServer.Models.TransferObjects;
using Latsic.IdServer.Models;
using Latsic.IdServer.Data;

namespace Latsic.IdServer.Controllers
{
  [Route("[controller]/[action]")]
  [ApiController]
  // [Authorize]
  public class User : ControllerBase
  {
    private readonly UserManager<IdUser> _userManager;
    private readonly IdUserDbContext _identityDbContext;

    public User(UserManager<IdUser> userManager, IdUserDbContext identityDbContext)
    {
      _userManager = userManager;
      _identityDbContext = identityDbContext;
    }

    [EnableCors("AllowSomeOrigins")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<UserBasicDto>>> Users()
    {
      var userData = _userManager.Users.Select(user => 
        new UserBasicDto{
          UserId = user.Id,
          UserName = user.UserName
        });
      
      return Ok(await _userManager.Users.Select(user => 
        new UserBasicDto{
          UserId = user.Id,
          UserName = user.UserName
        }).ToListAsync());
    }

    [HttpGet("{userId}")]
    [EnableCors("AllowSomeOrigins")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> Claims([FromRoute] string userId)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if(user == null) return notFoundUser(userId);

      return Ok((await _userManager.GetClaimsAsync(user)).Select(claim =>
        new ClaimDto
        {
          Type = claim.Type,
          Value = claim.Value
        }));
    }

    [HttpDelete("{userId}/{claimType}/{claimValue?}")]
    [EnableCors("AllowSomeOrigins")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ActionName("Claim")]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> ClaimDelete(
      [FromRoute] string userId, [FromRoute] string claimType, string claimValue = "")
    {
      var user = await _userManager.FindByIdAsync(userId);
      if(user == null) return notFoundUser(userId);
      
      var userClaimsToDelete = (await _userManager.GetClaimsAsync(user)).Where(claim =>
        claim.Type == claimType && (claim.Value == claimValue || string.IsNullOrEmpty(claimValue)));

      await _userManager.RemoveClaimsAsync(user, userClaimsToDelete);
      if(await _identityDbContext.SaveChangesAsync() <= 0)
      {
        return StatusCode(500, new {
          message = "Database error"
        });
      }

      return Ok(userClaimsToDelete.Select(claim =>
        new ClaimDto
        {
          Type = claim.Type,
          Value = claim.Value
        }));
    }

    [HttpPost("{userId}/{claimType}/{claimValue?}")]
    [EnableCors("AllowSomeOrigins")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ActionName("Claim")]
    public async Task<ActionResult<ClaimDto>> ClaimPost(
      [FromRoute] string userId, [FromRoute] string claimType, [FromRoute] string claimValue = "")
    {
      var user = await _userManager.FindByIdAsync(userId);
      if(user == null) return notFoundUser(userId);

      var userClaimsOfType = (await _userManager.GetClaimsAsync(user)).Where(claim => claim.Type == claimType);
      var result = new ClaimDto{ Type = claimType, Value = claimValue };

      foreach(var claim in userClaimsOfType)
      {
        if(claim.Value == claimValue)
        {
          return Ok(result);
        }
      }
      
      await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));

      if(await _identityDbContext.SaveChangesAsync() <= 0)
      {
        return StatusCode(500, new {
          message = "Database error"
        });
      }

      return Ok(result);
    }

    [HttpGet("{userId}/{claimType}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> Claim(
      [FromRoute] string userId, [FromRoute] string claimType)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if(user == null) return notFoundUser(userId);

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
      return NotFound(new {
        message = $"No user with id {userId}"
      });
    } 
  }
}



