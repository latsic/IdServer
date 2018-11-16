using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System.Threading.Tasks;
using System.Text;
using System;
using System.IO;

namespace Latsic.IdServer.Middleware
{
  public class RequestResponseLogging
  {
    private readonly RequestDelegate next;
    private readonly ILogger logger;

    public RequestResponseLogging(RequestDelegate next, ILoggerFactory loggerFactory)
    {
      this.next = next;
      logger = loggerFactory.CreateLogger<RequestResponseLogging>();
    }

    public async Task Invoke(HttpContext context)
    {
      context.Request.EnableRewind();

      var buffer = new byte[Convert.ToInt32(context.Request.ContentLength)];
      await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
      var requestBody = Encoding.UTF8.GetString(buffer);
      context.Request.Body.Seek(0, SeekOrigin.Begin);

      logger.LogInformation(requestBody);

      var originalBodyStream = context.Response.Body;

      using (var responseBody = new MemoryStream())
      {
        context.Response.Body = responseBody;

        await next(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        logger.LogInformation(response);
        await responseBody.CopyToAsync(originalBodyStream);
      }
    }
  }
}