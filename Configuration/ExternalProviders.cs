using System.Collections.Generic;

namespace Latsic.IdServer.Configuration
{
  public class Provider
  {
    public string Name { get; set; }
    public string Host { get; set; }
    public uint Port { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
  }
  
  public class ExternalProviders
  {
    public List<Provider> Providers { get; set; }
  }
}