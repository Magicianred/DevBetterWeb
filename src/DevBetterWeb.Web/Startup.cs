﻿using System;
using System.Collections.Generic;
using Ardalis.ListStartupServices;
using Autofac;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Infrastructure;
using DevBetterWeb.Infrastructure.Data;
using DevBetterWeb.Infrastructure.Services;
using GoogleReCaptcha.V3;
using GoogleReCaptcha.V3.Interface;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace DevBetterWeb.Web
{
  public class Startup
  {
    public const string DEFAULT_CONNECTION_STRING_NAME = "DefaultConnection";

    private bool _isDbContextAdded = false;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration config, IWebHostEnvironment env)
    {
      Configuration = config;
      _env = env;
    }

    public IConfiguration Configuration { get; }
    public ILifetimeScope? AutofacContainer { get; private set; }

    public void ConfigureProductionServices(IServiceCollection services)
    {
      if (!_isDbContextAdded)
      {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(Configuration
                .GetConnectionString(DEFAULT_CONNECTION_STRING_NAME)));
        _isDbContextAdded = true;
      }

      ConfigureServices(services);
    }

    public void ConfigureServices(IServiceCollection services)
    {
      services.Configure<CookiePolicyOptions>(options =>
      {
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
      });

      services.Configure<AuthMessageSenderOptions>(Configuration.GetSection("AuthMessageSenderOptions"));
      services.Configure<DiscordWebhookUrls>(Configuration.GetSection("DiscordWebhookUrls"));

      // TODO: Consider changing to check services collection for dbContext
      if (!_isDbContextAdded)
      {
        string dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(Configuration
                .GetConnectionString(DEFAULT_CONNECTION_STRING_NAME)));
        _isDbContextAdded = true;
      }

      services.AddMediatR(typeof(Startup).Assembly);

      services.AddMvc()
          .AddControllersAsServices()
          .AddRazorRuntimeCompilation();

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
      });

      services.AddScoped<IRepository, EfRepository>();
      //            services.Configure<AuthMessageSenderOptions>(Configuration);

      // list services
      services.Configure<ServiceConfig>(config =>
      {
        config.Services = new List<ServiceDescriptor>(services);
        config.Path = "/allmyservices";
      });

      services.AddHttpClient<ICaptchaValidator, GoogleReCaptchaValidator>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
      builder.RegisterModule(new DefaultInfrastructureModule(_env.EnvironmentName == "Development"));
    }

    public void Configure(IApplicationBuilder app,
        IWebHostEnvironment env,
        AppDbContext migrationContext)
    {
      if (env.EnvironmentName == "Development")
      {
        app.UseDeveloperExceptionPage();
        app.UseShowAllServicesMiddleware();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
      }

      app.UseHttpsRedirection();
      app.UseStaticFiles();
      //app.UseCookiePolicy();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
      });

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapRazorPages();
        endpoints.MapDefaultControllerRoute();
      });

      // run migrations automatically on startup
      //migrationContext.Database.Migrate();
      //migrationContext = null;

    }
  }
}
