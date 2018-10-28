
using System;

namespace Latsic.IdServer.Services
{
  public interface ICookieHandler
  {
    void SetBoolValue(string key, bool value);
    bool GetBoolValue(string key);
    void Commit();
  }
}