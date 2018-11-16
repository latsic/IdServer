namespace Latsic.IdServer.Configuration
{
  public class IdentityServerCertificate
  {
    public string FilePathPfx { get; set; }
    public string FilePathCer { get; set; }
    public string FilePathKey { get; set; }
    public string Password { get; set; }
  }
}