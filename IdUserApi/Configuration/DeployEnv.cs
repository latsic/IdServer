using System.Collections.Generic;

namespace Latsic.IdUserApi.Configuration
{
  public class DeployEnv
  {
    public bool ReverseProxy { get; set; }
    public string BasePath { get; set; }
  }
}