namespace Latsic.IdServer.Configuration
{
  public class CustomSettings
  {
    public string LocalClaimIssuer { get; set; }
    public string CookieSchemeUI { get; set; }
    public uint CookieUILifeTimeHours { get; set; }
  }
}