using System.Collections.Generic;

namespace Latsic.IdServer.Configuration
{
  public class WebClient
  {
    public string ClientId { get; set; }
    public string Uri { get; set; }
    public uint IdentityTokenLifetimeSeconds { get; set; }
    public uint AccessTokenLifetimeSeconds { get; set; }
  }
  
  public class WebClients
  {
    public List<WebClient> Clients { get; set; }
  }
}
