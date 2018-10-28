namespace Latsic.IdServer.Models.TransferObjects
{
  public class UserDtoOut
  {
    public string Id {get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public string EMail { get; set; }
    public string DateOfBirth { get; set; }
    public string Role { get; set; }
    public string UserNumber { get; set; }
  }
}