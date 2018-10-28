using System.ComponentModel.DataAnnotations;

namespace Latsic.IdServer.Models.TransferObjects
{
  public class UserDtoIn
  {
    // public int Id {get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    [Required(ErrorMessage="An email address is required")]
    public string EMail { get; set; }
    [Required(ErrorMessage="A password is required")]
    public string Password { get; set; }
    public string DateOfBirth { get; set; }
    public string Role { get; set; }
    public string UserNumber { get; set; }
  }
}