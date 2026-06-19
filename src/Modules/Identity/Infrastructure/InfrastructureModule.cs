using System.Reflection;
using System.Text;
using LabViroMol.Modules.Identity.Application.Roles.Queries;
using LabViroMol.Modules.Identity.Application.Users;
using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Application.Users.Queries;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Infrastructure.Services;
using LabViroMol.Modules.Identity.Infrastructure.Roles;
using LabViroMol.Modules.Identity.Infrastructure.Users;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LabViroMol.Modules.Identity.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHttpContextAccessor()
            .AddContext(configuration)
            .AddIdentityCore()
            .AddJwtAuthentication(configuration)
            .AddPermissionPolicies()
            .AddRepositories()
            .AddServices();

        return services;
    }

    private static IServiceCollection AddContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LabViroMol");

        services.AddDbContext<LabViroMolIdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__IdentityMigrationsHistory", "identity");
                npgsqlOptions.MigrationsAssembly(typeof(LabViroMolIdentityDbContext).Assembly.FullName);
            }));

        return services;
    }

    private static IServiceCollection AddIdentityCore(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
            .AddEntityFrameworkStores<LabViroMolIdentityDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token) &&
                            context.Request.Cookies.TryGetValue("X-Access-Token", out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        var permissionFields = typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        var authBuilder = services.AddAuthorizationBuilder();

        foreach (var permission in permissionFields)
        {
            if (permission.EndsWith($".{Permissions.Manage}"))
            {
                authBuilder.AddPolicy(permission, policy =>
                    policy.RequireClaim("permission", permission));
            }
            else if (permission.EndsWith($".{Permissions.View}"))
            {
                var managePermission = permission.Replace($".{Permissions.View}", $".{Permissions.Manage}");
                authBuilder.AddPolicy(permission, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("permission", permission) ||
                        context.User.HasClaim("permission", managePermission)));
            }
        }

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IRoleQueries, RoleQueries>();
        services.AddSingleton<IPermissionQueries, PermissionQueries>();
        return services;
    }
}
