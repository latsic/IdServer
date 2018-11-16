using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
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
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authorization;

using IdentityServer4;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using IdentityServer4.Extensions;
using IdentityServer4.EntityFramework.DbContexts;

using Latsic.IdServer.Configuration;
using Latsic.IdServer.Middleware;
using Latsic.IdServer.Services;

using Latsic.IdUserData.DataContexts;
using Latsic.IdUserData.Models;

namespace Latsic.IdServer
{
  public class Startup
  {
    private readonly ILogger<Startup> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostingEnvironment _hostingEnvironment;

    public Startup(
      IConfiguration configuration,
      IHostingEnvironment hostingEnvironment,
      ILogger<Startup> logger)
    {
      _configuration = configuration;
      _hostingEnvironment = hostingEnvironment;
      _logger = logger;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDbContext<IdUserDbContext>(options =>
        options.UseSqlite(_configuration.GetConnectionString("IdUserDb")));

      var customSettings = _configuration.GetSection("CustomSettings");
      services.Configure<CustomSettings>(customSettings);

      var externalProviders = _configuration.GetSection("ExternalProviders");
      services.Configure<ExternalProviders>(externalProviders);

      var deployEnv = _configuration.GetSection("DeployEnv");
      services.Configure<DeployEnv>(deployEnv);

      var webClients = _configuration.GetSection("WebClients");
      services.Configure<WebClients>(webClients);

      var idServerCertSettings = _configuration.GetSection("IdentityServerCertificate")
        .Get<IdentityServerCertificate>();

      services.AddCors(options =>
      {
        options.AddPolicy("AllowSomeOrigins", builder =>
        {
          var clients = webClients.Get<WebClients>();
          string[] uris = new string[clients.Clients.Count * 2];
          for(int i = 0; i < clients.Clients.Count; i += 2)
          {
            uris[i] = clients.Clients[i].Uri;
            uris[i + 1] = clients.Clients[i].Uri + "/";
          }
          builder.WithOrigins(uris).AllowAnyMethod().AllowAnyHeader();
        });
      });

      services.AddAutoMapper();
      services.AddSingleton<ICookieHandlerFactory, CookieHandlerFactory>();

      services.AddIdentity<IdUser, IdentityRole>()
        .AddEntityFrameworkStores<IdUserDbContext>()
        .AddDefaultTokenProviders();

      var mvcBbuilder = services.AddMvc();

      if(_hostingEnvironment.IsDevelopment())
      {
        mvcBbuilder.AddJsonOptions(options =>
        {
          options.SerializerSettings.Error =
            (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) =>
            {
              _logger.LogError("json serialisationerror", args);
              //Log args.ErrorContext.Error details...
            };
        });
      }
      mvcBbuilder.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

      var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

      var identityServerBuilder = services.AddIdentityServer(options =>
      {
        options.Authentication.CookieSlidingExpiration = true;
        options.Authentication.CookieLifetime = new TimeSpan(20, 0, 0, 0);
      });

      // if(true/*_hostingEnvironment.IsDevelopment()*/)
      // {
        identityServerBuilder = identityServerBuilder.AddDeveloperSigningCredential();
      // }
      // else
      // {
      //   var cert = new X509Certificate2(
      //     idServerCertSettings.FilePathPfx,
      //     idServerCertSettings.Password);

      //   identityServerBuilder = identityServerBuilder.AddSigningCredential(cert);
      // }
      
      identityServerBuilder.AddAspNetIdentity<IdUser>()
      .AddConfigurationStore(options =>
      {
        options.ConfigureDbContext = builder =>
          builder.UseSqlite(_configuration.GetConnectionString("IdServerDb"),
              sql => sql.MigrationsAssembly(migrationsAssembly));
      })
      .AddOperationalStore(options =>
      {
        options.ConfigureDbContext = builder =>
          builder.UseSqlite(_configuration.GetConnectionString("IdServerDb"),
            sql => sql.MigrationsAssembly(migrationsAssembly));

        // this enables automatic token cleanup. this is optional.
        options.EnableTokenCleanup = true;
        options.TokenCleanupInterval = 30;
      });


      var serviceCollection = services.AddAuthentication();
      foreach (var provider in externalProviders.Get<ExternalProviders>().Providers)
      {
        if (provider.Name == "Google")
        {
          try {
            serviceCollection.AddGoogle(provider.Name, options =>
            {
              options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

              options.ClientId = provider.ClientId;
              options.ClientSecret = provider.ClientSecret;
            });
          }
          catch(Exception e)
          {
            _logger.LogError("Error adding google as external authentication provider", e);
          }
        }
      }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(
      IApplicationBuilder app,
      IHostingEnvironment env,
      IOptions<DeployEnv> deployEnv,
      IOptions<WebClients> webClients,
      ILoggerFactory loggerFactory)
    {
      // loggerFactory.AddFile("IdServer-{Date}.txt");

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      //app.UsePathBase(new PathString(hostingEnv.Value.BasePath));
      
      if(!string.IsNullOrWhiteSpace(deployEnv.Value.BasePath))
      {
        app.Use(async (ContextBoundObject, next) =>
        {
          ContextBoundObject.Request.PathBase = deployEnv.Value.BasePath;
          await next.Invoke();
        });
      }

      if(deployEnv.Value.ReverseProxy)
      {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
          ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
      }

      app.UseCors("AllowSomeOrigins");

      InitializeDbIdServer(app, webClients);
      InitializeDbIdUser(app);

      app.UseStaticFiles();
      app.UseIdentityServer();
      
      if (env.IsDevelopment())
      {
        app.UseMiddleware<RequestResponseLogging>();
      }

      app.UseMvc(routes =>
      { 
        routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
      });
    }

    private void InitializeDbIdUser(IApplicationBuilder app)
    {
      using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
      {
        var idUserDbContext = serviceScope.ServiceProvider.GetRequiredService<IdUserDbContext>();
        idUserDbContext.Database.Migrate();
      }
    }

    private void InitializeDbIdServer(IApplicationBuilder app, IOptions<WebClients> webClients)
    {

      var idServerInitialData = new IdServerInitialData(webClients);
      
      using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
      {
        serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

        var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.Database.Migrate();

        var existingClients = context.Clients.ToList();
        context.Clients.RemoveRange(existingClients);
        existingClients.RemoveAll(existingClient => true);

        foreach (var client in idServerInitialData.GetClients())
        {
          if (!existingClients.Any(existingClient => existingClient.ClientId == client.ClientId))
          {
            context.Clients.Add(client.ToEntity());
          }
        }
        context.SaveChanges();

        var existingIdentityResources = context.IdentityResources.ToList();
        context.IdentityResources.RemoveRange(existingIdentityResources);
        existingIdentityResources.RemoveAll(existingIdentityResource => true);

        foreach (var identityResource in idServerInitialData.IdentityResources.Values)
        {
          if(!existingIdentityResources.Any(
            existingIdentityResource => existingIdentityResource.Name == identityResource.Name))
          {
            context.IdentityResources.Add(identityResource.ToEntity());
          }
        }
        context.SaveChanges();

        var existingApiResources = context.ApiResources.ToList();
        context.ApiResources.RemoveRange(existingApiResources);
        existingApiResources.RemoveAll(existingApiResource => true);

        foreach (var apiResouce in idServerInitialData.ApiResources.Values)
        {
          if (!existingApiResources.Any(existingApiResource => existingApiResource.Name == apiResouce.Name))
          {
            context.ApiResources.Add(apiResouce.ToEntity());
          }
        }
        context.SaveChanges();
      }
    }
  }

}
