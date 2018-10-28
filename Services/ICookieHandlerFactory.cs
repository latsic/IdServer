using Microsoft.AspNetCore.Http;

namespace Latsic.IdServer.Services
{
  public interface ICookieHandlerFactory
  {
    ICookieHandler CreateInstance(IHttpContextAccessor httpContextAccessor);
  }
}