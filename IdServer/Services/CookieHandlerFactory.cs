using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Latsic.IdServer.Configuration;

namespace Latsic.IdServer.Services
{
  public class CookieHandlerFactory : ICookieHandlerFactory
  {
    private readonly CustomSettings _customSettings;

    public CookieHandlerFactory(IOptions<CustomSettings> customSettings)
    {
      _customSettings = customSettings.Value;
    }

    public ICookieHandler CreateInstance(IHttpContextAccessor httpContextAccessor)
    {
      return new CookieHandler(
        httpContextAccessor,
        _customSettings.CookieSchemeUI,
        _customSettings.CookieUILifeTimeHours);
    }
  }
}