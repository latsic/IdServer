using Microsoft.AspNetCore.Identity;

namespace Latsic.IdServer.Models
{
  // Add profile data for application users by adding properties to the IdUser class
  public class IdUser : IdentityUser
  {
    public string CustomProp { get; set; } = "testProp";
    public string SecondCustomProp { get; set; } = "secondTestProp";
  }
}
