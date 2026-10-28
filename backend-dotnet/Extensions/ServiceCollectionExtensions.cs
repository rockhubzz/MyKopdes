using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MyKopdes.Configuration;

namespace MyKopdes.Middleware;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        services.Configure<JwtSettings>(jwtSettings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
            };
        });

        return services;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        // Restrict CORS to the configured app URL (same-origin deployment via
        // nginx means the browser never needs cross-origin access). Wildcard
        // + AllowCredentials previously allowed any site to make credentialed
        // requests on behalf of a logged-in user.
        var allowedOrigin = Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost";
        app.UseCors(policy =>
        {
            policy.WithOrigins(allowedOrigin)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
        return app;
    }

    public static IServiceCollection AddRoleAuthorization(this IServiceCollection services)
    {
        // Roles: admin, owner, manager (staff), member
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            options.AddPolicy("OwnerOnly", policy => policy.RequireRole("owner"));
            options.AddPolicy("ManagerOnly", policy => policy.RequireRole("manager"));
            options.AddPolicy("StaffOnly", policy => policy.RequireRole("admin", "owner", "manager"));
            options.AddPolicy("MemberOnly", policy => policy.RequireRole("member"));
            options.AddPolicy("StaffOrMember", policy => policy.RequireRole("admin", "owner", "manager", "member"));
        });

        return services;
    }
}
