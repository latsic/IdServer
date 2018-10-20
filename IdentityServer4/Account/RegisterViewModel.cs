// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;

namespace Latsic.IdServer.UI
{
  public class RegisterViewModel : RegisterInputModel
  {
    public RegisterViewModel(RegisterInputModel parent)
    {
      Username = parent.Username;
      Password = parent.Password;
      PasswordRepeated = parent.PasswordRepeated;
    }
    public RegisterViewModel()
    {

    }

    public string Title { get; set; } = "Register a local account";

    public bool CanRegister
    {
      get
      {
        if(Username == null || Password == null || PasswordRepeated == null)
        {
          return false;
        }
        return Username.Length > 0 && Password.Length > 0 && Password == PasswordRepeated;
      }
    }
  }
}