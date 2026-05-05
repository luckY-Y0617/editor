using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Northstar.Api.Infrastructure;
using Northstar.Api.Middleware;
using Northstar.Api.Security;
using Northstar.Application;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Infrastructure;
using Northstar.Infrastructure.Persistence;
using Northstar.Infrastructure.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IAuthRequestContext, HttpAuthRequestContext>();
builder.Services.AddScoped<IScimBearerTokenAccessor, HttpScimBearerTokenAccessor>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            return;
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => builder.Environment.IsDevelopment());
    });
});

builder.Services.AddControllers(options =>
{
    options.Conventions.Insert(0, new RoutePrefixConvention("api/v1"));
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var fields = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => string.IsNullOrWhiteSpace(entry.Key) ? "$" : entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The value is invalid."
                        : error.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse(new ApiError(
            ErrorCodes.ValidationError,
            "Request validation failed.",
            new { fields })))
        {
            ContentTypes = { "application/json" }
        };
    };
});

var jwtOptions = builder.Configuration.GetSection($"{AuthOptions.SectionName}:Jwt").Get<AuthOptions.JwtOptions>() ?? new();
var signingKey = jwtOptions.SigningKey;
if (string.IsNullOrWhiteSpace(signingKey) &&
    (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")))
{
    signingKey = "northstar-local-development-signing-key-change-me";
}

if (string.IsNullOrWhiteSpace(signingKey))
{
    throw new InvalidOperationException("Auth:Jwt:SigningKey is required.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await AuthErrorWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    ErrorCodes.Unauthorized,
                    "Authentication is required.");
            },
            OnForbidden = async context =>
            {
                await AuthErrorWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    ErrorCodes.Forbidden,
                    "Workspace permission is insufficient.");
            }
        };
    });
builder.Services.AddAuthorization();

var publicShareOptions = builder.Configuration
    .GetSection(PermissionPublicShareOptions.SectionName)
    .Get<PermissionPublicShareOptions>() ?? new PermissionPublicShareOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(PublicShareRateLimitPolicyNames.PublicShareLinks, context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = Math.Max(1, publicShareOptions.RateLimit.PermitLimit),
                QueueLimit = Math.Max(0, publicShareOptions.RateLimit.QueueLimit),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromSeconds(Math.Max(1, publicShareOptions.RateLimit.WindowSeconds))
            });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Northstar API",
        Version = "v1"
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NorthstarDbContext>("postgresql");

var app = builder.Build();

await app.Services.InitializeNorthstarDatabaseAsync(app.Configuration);

app.UseMiddleware<ApiExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/v1/health");
app.MapControllers();

app.Run();

public partial class Program;
