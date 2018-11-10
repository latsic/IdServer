using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using IdentityModel;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.EntityFrameworkCore;



using IdentityServer4.Stores;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.Entities;

using Latsic.IdServer.Models.TransferObjects;

namespace Latsic.IdServer.Controllers
{
  [Route("[controller]/[action]")]
  [ApiController]
  public class TokenConfigController : ControllerBase
  {
    private readonly IClientStore _clientStore;
    private readonly ConfigurationDbContext _dbContext;
    
    public TokenConfigController(IClientStore clientStore, ConfigurationDbContext dbContext)
    {
      _clientStore = clientStore;
      _dbContext = dbContext;
    }

    [HttpGet("{clientId}")]
    [EnableCors("AllowSomeOrigins")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    // [Authorize]
    public async Task<ActionResult<AccessTokenConfigDto>> AccessTokenConfig([FromRoute] string clientId)
    {
      var client = await _clientStore.FindClientByIdAsync(clientId);
      if(client == null)
      {
        return NotFound(new {
          message = $"No client with id {clientId} found."
        });
      }

      return Ok(new AccessTokenConfigDto
      {
        LifeTimeSeconds = client.AccessTokenLifetime
      });
    }

    [HttpPut("{clientId}")]
    [EnableCors("AllowSomeOrigins")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    // [Authorize]
    public async Task<ActionResult<AccessTokenConfigDto>> AccessTokenConfig(
      [FromRoute] string clientId, [FromBody] AccessTokenConfigDto accessTokenConfig)
    {
      if(accessTokenConfig.LifeTimeSeconds <= 0) {
        return BadRequest(new {
          message = $"Negative access token lifetime is not allowed."
        });
      }

      if(accessTokenConfig.LifeTimeSeconds > 3600) {
        return BadRequest(new {
          message = $"Max allowed lifetime of access tokens is 1 hour."
        });
      }

      var client = await _dbContext.Clients.SingleOrDefaultAsync(c => c.ClientId == clientId);

      //var client = await _clientStore.FindClientByIdAsync(clientId);
      if(client == null)
      {
        return NotFound(new {
          message = $"No client with id {clientId} found."
        });
      }
      
      if(client.AccessTokenLifetime == accessTokenConfig.LifeTimeSeconds) {
        return Ok(accessTokenConfig);
      }

      client.AccessTokenLifetime = accessTokenConfig.LifeTimeSeconds;
     
      if(await _dbContext.SaveChangesAsync() > 0)
      {
        return Ok(accessTokenConfig);
      }

      return StatusCode(500, new {
        message = "Database error"
      });
    }
  }
}


