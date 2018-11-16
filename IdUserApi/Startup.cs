using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;

using IdentityModel;
using IdentityServer4.AccessTokenValidation;

using NSwag.AspNetCore;
using NJsonSchema;

using Latsic.IdUserData.DataContexts;
using Latsic.IdUserData.Models;
using Latsic.IdUserApi.Configuration;

namespace IdUserApi
{
  public class Startup
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<Startup> _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    public void ConfigureServices(IServiceCollection services)
    {
      
      var apiSettingsSection = _configuration.GetSection("ApiSettings");
      var apiSettings = apiSettingsSection.Get<ApiSettings>();
      services.Configure<ApiSettings>(apiSettingsSection);
      
      var deployEnv = _configuration.GetSection("DeployEnv");
      services.Configure<DeployEnv>(deployEnv);
      
      services.AddDbContext<IdUserDbContext>(options =>
        options.UseSqlite(_configuration.GetConnectionString("IdUserDb")));

      //services.AddIdentity<IdUser, IdentityRole>()
      //  .AddEntityFrameworkStores<IdUserDbContext>()
      //  .AddDefaultTokenProviders();
      services.AddIdentityCore<IdUser>()
        .AddEntityFrameworkStores<IdUserDbContext>()
        .AddDefaultTokenProviders();

      services.AddAuthorization(options =>
      {
        options.AddPolicy("Admin", policy => policy.RequireClaim(JwtClaimTypes.Role, new string[] {"admin", "Admin", "ADMIN"}));
        options.AddPolicy("ApiAccess", policy => policy.RequireClaim(CustomClaims.ApiAccess, apiSettings.ApiName));
      });

      services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
      .AddIdentityServerAuthentication(options =>
      {
        options.Authority = apiSettings.AuthAuthorityUrl;
        options.ApiName = apiSettings.ApiName;
        options.RequireHttpsMetadata = false;
      });

      services.AddCors(options =>
      {
        options.AddPolicy("AllowAllOrigins", builder =>
        {
          builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
      });

      

      services.AddSwagger();

      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
    }

    public void Configure(
      IApplicationBuilder app,
      IHostingEnvironment env,
      IOptions<DeployEnv> deployEnv)
    {
      if(env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else if(env.IsProduction() && !deployEnv.Value.ReverseProxy)
      {
        app.UseHsts();
        app.UseHttpsRedirection();
      }

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
     
      app.UseCors("AllowAllOrigins");
      app.UseAuthentication();

      InitializeDbIdUser(app);

      // Register the Swagger generator and the Swagger UI middlewares
      app.UseSwaggerUi3WithApiExplorer(settings =>
      {
        settings.GeneratorSettings.DefaultPropertyNameHandling = 
          PropertyNameHandling.CamelCase;
        
        if(env.IsProduction())
        {
          settings.PostProcess = document => document.BasePath = deployEnv.Value.BasePath + "/";
        }
        settings.GeneratorSettings.Title = "Account creation, admin and info endpoints";
        settings.GeneratorSettings.Description = "An API create and manage users";
      });

      app.UseMvc();
    }

    private void InitializeDbIdUser(IApplicationBuilder app)
    {
      using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
      {
        var idUserDbContext = serviceScope.ServiceProvider.GetRequiredService<IdUserDbContext>();
        idUserDbContext.Database.Migrate();
      }
    }
  }
}
