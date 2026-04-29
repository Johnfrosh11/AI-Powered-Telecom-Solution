using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NaijaShield.Application;
using NaijaShield.Infrastructure;
using NaijaShield.Api.Hubs;
using NaijaShield.Api.Middleware;
using NaijaShield.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .WriteTo.Console());

// ── MVC / Controllers ─────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── JWT Authentication ────────────────────────────────────────────────────────

var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT:SecretKey not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };

        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Application + Infrastructure ─────────────────────────────────────────────

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<NaijaShield.Application.Common.Interfaces.IRealtimeNotifier,
    NaijaShield.Api.Hubs.RealtimeNotifier>();

// ── SignalR ───────────────────────────────────────────────────────────────────

builder.Services.AddSignalR();

// ── Hangfire ──────────────────────────────────────────────────────────────────

builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// ── CORS ──────────────────────────────────────────────────────────────────────

builder.Services.AddCors(opts =>
    opts.AddPolicy("NaijaShieldPolicy", p =>
        p.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// ── Rate Limiting ─────────────────────────────────────────────────────────────

builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("api", cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 10;
    });
});

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NaijaShield AI API",
        Version = "v1",
        Description = "Enterprise fraud detection API for Nigerian telecoms"
    });

    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────

var healthBuilder = builder.Services.AddHealthChecks();
if (!builder.Environment.IsEnvironment("Testing"))
    healthBuilder.AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// ── App Pipeline ──────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NaijaShield AI v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("NaijaShieldPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");

app.MapHub<FraudHub>("/hubs/fraud");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<ConversationsHub>("/hubs/conversations");
app.MapHub<SystemHub>("/hubs/system");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
});

app.MapHealthChecks("/health");

// ── Database seed (dev + staging) ─────────────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Staging"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();

// Expose Program class for integration tests
public partial class Program { }

