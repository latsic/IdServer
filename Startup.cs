﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

using IdentityServer4;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using IdentityServer4.Extensions;
using IdentityServer4.EntityFramework.DbContexts;

using Latsic.IdServer.Configuration;
using Latsic.IdServer.Data;
using Latsic.IdServer.Models;

namespace Latsic.IdServer
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDbContext<IdUserDbContext>(options =>
        options.UseSqlite(Configuration.GetConnectionString("IdUserDb")));

      var customSettings = Configuration.GetSection("CustomSettings");
      services.Configure<CustomSettings>(customSettings);

      var externalProviders = Configuration.GetSection("ExternalProviders");
      services.Configure<ExternalProviders>(externalProviders);

      services.AddAutoMapper();

      services.AddIdentity<IdUser, IdentityRole>()
        .AddEntityFrameworkStores<IdUserDbContext>()
        .AddDefaultTokenProviders();

      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
     
      var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

      services.AddIdentityServer()
      .AddDeveloperSigningCredential()
      .AddAspNetIdentity<IdUser>()
      .AddConfigurationStore(options =>
      {
        options.ConfigureDbContext = builder =>
          builder.UseSqlite(Configuration.GetConnectionString("IdServerDb"),
              sql => sql.MigrationsAssembly(migrationsAssembly));
      })
      .AddOperationalStore(options =>
      {
        options.ConfigureDbContext = builder =>
          builder.UseSqlite(Configuration.GetConnectionString("IdServerDb"),
            sql => sql.MigrationsAssembly(migrationsAssembly));

        // this enables automatic token cleanup. this is optional.
        options.EnableTokenCleanup = true;
        options.TokenCleanupInterval = 30;
      });


      var serviceCollection = services.AddAuthentication();
      foreach(var provider in externalProviders.Get<ExternalProviders>().Providers)
      {
        if(provider.Name == "Google")
        {
          serviceCollection.AddGoogle(provider.Name, options => {
            options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

            options.ClientId = provider.ClientId;
            options.ClientSecret = provider.ClientSecret;
          });
        }
      }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      InitializeDatabase(app);

      app.UseStaticFiles();
      app.UseIdentityServer();
      //app.UseMvcWithDefaultRoute();

      app.UseMvc(routes =>
      {
        routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
      });
    }

    private void InitializeDatabase(IApplicationBuilder app)
    {
      using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
      {
        serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

        var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.Database.Migrate();

        var existingClients = context.Clients.ToList();
        context.Clients.RemoveRange(existingClients);
        existingClients.RemoveAll(existingClient => true);

        foreach(var client in Config.GetClients())
        {
          if(!existingClients.Any(existingClient => existingClient.ClientId == client.ClientId))
          {
            context.Clients.Add(client.ToEntity());
          }
        }
        context.SaveChanges();
        
        var existingIdentityResources = context.IdentityResources.ToList();
        context.IdentityResources.RemoveRange(existingIdentityResources);
        existingIdentityResources.RemoveAll(existingIdentityResource => true);

        foreach(var identityResource in Config.GetIdentityResources())
        {
          if(!existingIdentityResources.Any(existingIdentityResource => existingIdentityResource.Name == identityResource.Name))
          {
            context.IdentityResources.Add(identityResource.ToEntity());
          }
        }
        context.SaveChanges();

        var existingApiResources = context.ApiResources.ToList();
        context.ApiResources.RemoveRange(existingApiResources);
        existingApiResources.RemoveAll(existingApiResource => true);

        foreach(var apiResouce in Config.GetApiResources())
        {
          if(!existingApiResources.Any(existingApiResource => existingApiResource.Name == apiResouce.Name))
          {
            context.ApiResources.Add(apiResouce.ToEntity());
          }
        }
      }
    }
  }
}