using System;

namespace Latsic.IdServer.Errors
{
  public class IdServerException : Exception
  {
    public enum Type
    {
      Unspecified
    }

    public IdServerException()
    {
    }

    public IdServerException(string msg) : base(msg)
    {
    }

    public IdServerException(string msg, Exception inner) : base(msg, inner)
    {
    }

    public Type Reason { get; set; } = Type.Unspecified;

  }
}