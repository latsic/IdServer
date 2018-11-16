using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace Latsic.IdUserApi.Controllers
{
  [Route("/")]
  [ApiController]
  [EnableCors("AllowAllOrigins")]
  [ApiExplorerSettings(IgnoreApi = true)]
  public class RedirectController : ControllerBase
  {
    [HttpGet]
    [ProducesResponseType(302)]
    public ActionResult<LocalRedirectResult> RedirectToSwagger()
    {
      return Redirect("~/swagger");
    }
  }
}