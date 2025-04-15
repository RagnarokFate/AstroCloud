using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AstroCloud.Data;
using AstroCloud.Data.Interfaces;
using AstroCloud.Data.Repositories;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.MySql;
using Microsoft.OpenApi.Models;
using AstroCloud.Data.Interfaces.AstroCloud.Data.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with enhanced settings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/astrocloud-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Building application services...");

    // Add services to the container
    var connectionString = builder.Configuration.GetConnectionString("DatabaseLinkConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured");
    }

    // Database Context with retry policy
    builder.Services.AddDbContext<AppDatabaseContext>(options =>
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), 
            mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    });

    // Enhanced Health Checks
    builder.Services.AddHealthChecks()
        .AddMySql(connectionString, name: "MySQL Health")
        .AddDbContextCheck<AppDatabaseContext>();


    // Controllers with JSON options
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.WriteIndented = true;
        });

    // Swagger with JWT support
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "AstroCloud API", Version = "v1" });
        
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Repository Pattern
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<AuthService>();

    // JWT Authentication with enhanced validation
    var jwtConfig = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig["Issuer"],
                ValidAudience = jwtConfig["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!)),
                ClockSkew = TimeSpan.Zero
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Error("Authentication failed: {Exception}", context.Exception);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Information("User {Username} authenticated", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    // Apply pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDatabaseContext>();
        await dbContext.Database.MigrateAsync();
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AstroCloud API v1");
            c.DisplayRequestDuration();
        });
        
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    
    // Custom error handling middleware
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception occurred");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "An unexpected error occurred",
                Details = builder.Environment.IsDevelopment() ? ex.Message : null
            });
        }
    });

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Enhanced health check endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                Status = report.Status.ToString(),
                Duration = report.TotalDuration,
                Checks = report.Entries.Select(e => new
                {
                    Component = e.Key,
                    Status = e.Value.Status.ToString(),
                    Duration = e.Value.Duration,
                    Error = e.Value.Exception?.Message
                })
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    // Simple ping endpoint
    app.MapGet("/ping", () => Results.Ok("pong"));

    // Database connection test endpoint
    app.MapGet("/db-check", async (AppDatabaseContext db) =>
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            return Results.Ok(new
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                Database = db.Database.GetDbConnection().Database,
                Server = db.Database.GetDbConnection().DataSource
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Database check failed: {ex.Message}");
        }
    });

    app.MapControllers();

    Log.Information("Starting AstroCloud application...");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Database: {ConnectionString}", connectionString);

    await app.RunAsync();
}
catch (Exception ex) when (ex.GetType().Name is not "StopTheHostException" && ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.Information("Shutting down application...");
    await Log.CloseAndFlushAsync();
}