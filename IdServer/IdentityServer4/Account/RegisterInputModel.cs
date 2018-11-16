// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Latsic.IdServer.UI
{
  public class RegisterInputModel
  {
    [Required]
    public string Username { get; set; }
    [Required]
    public string Password { get; set; }
    public string PasswordRepeated { get; set; }
    public string ReturnUrl { get; set; }
  }
}