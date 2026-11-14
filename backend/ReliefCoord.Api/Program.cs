using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReliefCoord.Api.Security;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Application.Services;
using ReliefCoord.Application.Strategies;
using ReliefCoord.Infrastructure.ExternalServices;
using ReliefCoord.Infrastructure.Messaging;
using ReliefCoord.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI-Powered Humanitarian Crisis Management & Aid Coordination API",
        Version = "v1.0",
        Description = "SE205.3 Software Architecture Multi-Organisation Humanitarian Operations Platform"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

string dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
builder.Services.AddDbContext<ReliefCoordDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection"));
    }
    else if (dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase("ReliefCoordTestDb");
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=reliefcoord_platform.db");
    }
});

string jwtKey = builder.Configuration["Jwt:Key"] ?? "HumanitarianCrisisPlatformSuperSecretSigningKey2026!@#998877";
string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ReliefCoordApi";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ReliefCoordClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IServiceResilienceMonitor, ServiceResilienceMonitor>();
builder.Services.AddSingleton<InMemoryDomainEventQueue>();
builder.Services.AddSingleton<IDomainEventPublisher>(sp => sp.GetRequiredService<InMemoryDomainEventQueue>());
builder.Services.AddHostedService<AsynchronousDomainEventWorker>();

builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IMappingProviderService, ResilientMappingProviderService>();
builder.Services.AddScoped<IAiProviderFactory, AiProviderFactory>();
builder.Services.AddScoped<INotificationChannelFactory, NotificationChannelFactory>();

builder.Services.AddScoped<IResourceAllocationStrategy, MultiConstraintOptimalStrategy>();
builder.Services.AddScoped<IResourceAllocationStrategy, ShortageProportionalContentionStrategy>();
builder.Services.AddScoped<IResourceAllocationStrategy, ProximityFirstStrategy>();
builder.Services.AddScoped<ResourceAllocationStrategyResolver>();

builder.Services.AddScoped<HumanitarianOperationsService>();
builder.Services.AddSingleton<JwtTokenGenerator>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        await context.Response.WriteAsJsonAsync(new
        {
            error = exception?.GetType().Name ?? "Error",
            message = exception?.Message ?? "An unexpected error occurred."
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReliefCoordDbContext>();
    await DatabaseSeeder.EnsureCleanProductionDatabaseAsync(db);
}

app.Run();

public partial class Program { }
