using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Latsic.IdServer.Models;

namespace Latsic.IdServer.Data
{
  public class IdUserDbContext : IdentityDbContext<
    IdUser, IdentityRole, string,
    IdUserClaim, IdentityUserRole<string>,
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>
  {
    public IdUserDbContext(DbContextOptions<IdUserDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<IdUser>(b =>
    {
        b.ToTable("Users");
    });

    modelBuilder.Entity<IdUserClaim>(b =>
    {
        b.ToTable("Claims");
    });

    modelBuilder.Entity<IdentityUserLogin<string>>(b =>
    {
        b.ToTable("ExternalLogins");
    });

    modelBuilder.Entity<IdentityUserToken<string>>(b =>
    {
        b.ToTable("Tokens");
    });

    modelBuilder.Entity<IdentityRole>(b =>
    {
        b.ToTable("Roles");
    });

    modelBuilder.Entity<IdentityRoleClaim<string>>(b =>
    {
        b.ToTable("RoleClaims");
    });

    modelBuilder.Entity<IdentityUserRole<string>>(b =>
    {
        b.ToTable("UserRoles");
    });
    }
  }
}
