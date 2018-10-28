using System;
using Microsoft.AspNetCore.Http;

using Latsic.IdServer.Utitlies;

namespace Latsic.IdServer.Services
{
  public class CookieHandler : ICookieHandler
  {
    private IRequestCookieCollection _requestCookies;
    private IResponseCookies _responseCookies;
    private string _cookieName;
    private uint _cookieLifeTimeHours;
    private DictJson _dictJsonCookie;
    private bool _requestHasCookie;
    
    public CookieHandler(
      IHttpContextAccessor httpContextAccessor, string cookieName, uint cookieLifeTimeHours)
    {
      if(string.IsNullOrWhiteSpace(cookieName))
      {
        throw new ArgumentException("does not contain valid cookie name", cookieName);
      }
      
      _requestCookies = httpContextAccessor.HttpContext.Request.Cookies;
      _responseCookies = httpContextAccessor.HttpContext.Response.Cookies;
      _cookieName = cookieName;
      _cookieLifeTimeHours = cookieLifeTimeHours;
      _requestHasCookie = _requestCookies.ContainsKey(_cookieName);

      _dictJsonCookie = _requestHasCookie
        ? new DictJson(_requestCookies[_cookieName])
        : new DictJson();
    }

    public void SetBoolValue(string key, bool value)
    {
      if(value)
      {
        _dictJsonCookie.SetValue(key, value);
      }
      else
      {
        _dictJsonCookie.RemoveValue(key);
      }
    }

    public void SetValue(string key, dynamic value)
    {
      if(value == null) {
        _dictJsonCookie.RemoveValue(key);
      }
      else
      {
        _dictJsonCookie.SetValue(key, value);
      }
    }

    public bool GetBoolValue(string key)
    {
      return _dictJsonCookie.HasValue(key);
    }

    public dynamic GetValue(string key)
    {
      return _dictJsonCookie.GetValue(key);
    }

    public void Commit()
    {
      if(_dictJsonCookie.IsEmpty && _requestHasCookie)
      {
        _responseCookies.Delete(_cookieName);
      }
      else
      {
        _responseCookies.Append(_cookieName, _dictJsonCookie.Json, new CookieOptions
        {
          HttpOnly = true,
          Expires = DateTime.Now.AddHours(_cookieLifeTimeHours)
        });
      }
    }

  }
}