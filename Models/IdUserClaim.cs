using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Latsic.IdServer.Models
{
  // Add profile data for application users by adding properties to the IdUser class
  public class IdUserClaim : IdentityUserClaim<string>
  {
    
    public string Issuer { get; set; }

    //
    // Summary:
    //     Reads the type and value from the Claim.
    //
    // Parameters:
    //   claim:
    public override void InitializeFromClaim(Claim claim)
    {
      base.InitializeFromClaim(claim);
      Issuer = claim.Issuer;
    }
    //
    // Summary:
    //     Converts the entity into a Claim instance.
    public override Claim ToClaim()
    {
      var claim = base.ToClaim();
      return new Claim(claim.Type, claim.Value, claim.ValueType, Issuer, claim.OriginalIssuer, claim.Subject);
    }
  }
}
