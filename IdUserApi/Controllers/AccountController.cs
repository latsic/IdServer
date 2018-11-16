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
using Microsoft.AspNetCore.Cors;

using Latsic.IdUserApi.Models.TransferObjects;
using Latsic.IdUserData.Models;
using Latsic.IdUserData.DataContexts;

namespace Latsic.IdUserApi.Controllers
{
  [Route("[controller]/[action]")]
  [ApiController]
  [EnableCors("AllowAllOrigins")]
  [Authorize]
  public class Account : ControllerBase
  {
    private readonly UserManager<IdUser> _userManager;
    private readonly IdUserDbContext _identityDbContext;

    public Account(UserManager<IdUser> userManager, IdUserDbContext identityDbContext)
    {
      _userManager = userManager;
      _identityDbContext = identityDbContext;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<UserDtoOut>> Register([FromBody] UserDtoIn userDtoIn)
    {
      if(string.IsNullOrWhiteSpace(userDtoIn.EMail))
      {
        return BadRequest(new {
          message = "Invalid Email Address"
        });
      }

      if(string.IsNullOrWhiteSpace(userDtoIn.Password))
      {
        return BadRequest(new {
          message = "Invalid password"
        });
      }

      if(string.IsNullOrWhiteSpace(userDtoIn.UserName))
      {
        userDtoIn.UserName = userDtoIn.EMail;
      }

      IdUser user = new IdUser
      {
        UserName = userDtoIn.UserName,
        Email = userDtoIn.EMail,
      };

      var result = await _userManager.CreateAsync(user, userDtoIn.Password);
      var userDtoOut = new UserDtoOut();
      string name = "";
      if(result.Succeeded)
      {
        var claims = new List<Claim>();
        
        if(!string.IsNullOrWhiteSpace(userDtoIn.FirstName))
        {
          claims.Add(new Claim(JwtClaimTypes.GivenName, userDtoIn.FirstName));
          userDtoOut.FirstName = userDtoIn.FirstName;
          name += userDtoIn.FirstName;
        }
        if(!string.IsNullOrWhiteSpace(userDtoIn.LastName))
        {
          claims.Add(new Claim(JwtClaimTypes.FamilyName, userDtoIn.LastName));
          userDtoOut.LastName = userDtoIn.LastName;
          if(name.Count() > 0) name += " " + userDtoOut.LastName;
          else name += userDtoIn.LastName;
        }
        if(!string.IsNullOrWhiteSpace(userDtoIn.DateOfBirth))
        {
          claims.Add(new Claim(JwtClaimTypes.BirthDate, userDtoIn.DateOfBirth));
          userDtoOut.DateOfBirth = userDtoIn.DateOfBirth;
        }
        if(!string.IsNullOrWhiteSpace(userDtoIn.Role))
        {
          claims.Add(new Claim(JwtClaimTypes.Role, userDtoIn.Role));
          userDtoOut.Role = userDtoIn.Role;
        }

        if(!string.IsNullOrWhiteSpace(userDtoIn.UserNumber))
        {
          claims.Add(new Claim(CustomClaims.UserNumber, userDtoIn.UserNumber));
          userDtoOut.UserNumber = userDtoIn.UserNumber;
        }
        
        // Add claim to access to IdApi1 and IdUserApi for all users per default.
        claims.Add(new Claim(CustomClaims.ApiAccess, "IdApi1"));
        claims.Add(new Claim(CustomClaims.ApiAccess, "IdUserApi"));

        if(userDtoIn.UserName != userDtoIn.EMail)
        {
          name = userDtoIn.UserName;
        }
        if(name.Count() == 0)
        {
          name = user.Id;
        }
        claims.Add(new Claim(JwtClaimTypes.Name, name));

        result = await _userManager.AddClaimsAsync(user, claims);

        await _identityDbContext.SaveChangesAsync();
      }
      if(result.Succeeded)
      {
        userDtoOut.Id = user.Id;
        userDtoOut.UserName = user.UserName;
        userDtoOut.EMail = user.Email;
        return StatusCode(201, userDtoOut);
      }
      return handleIdentityError(result.Errors, user);
    }

    private ActionResult<UserDtoOut> handleIdentityError(IEnumerable<IdentityError> errors, IdUser user)
    {
      if(errors.Count() == 0)
      {
        return BadRequest(new {
          message = "Unknown Error"
        });
      }

      foreach(var error in errors)
      {
        if(error.Code == new IdentityErrorDescriber().DuplicateUserName(user.UserName).Code)
        {
          return Conflict(new {
            message = $"The username {user.UserName} is already in use"
          });
        }

      }
      return BadRequest(new {
        message = errors.First().Description
      });
    }
  }
}


