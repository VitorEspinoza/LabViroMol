using LabViroMol.Modules.Identity.Application.Users.ChangePassword;
using LabViroMol.Modules.Identity.Application.Users.CreateUser;
using LabViroMol.Modules.Identity.Application.Users.DeactivateUser;
using LabViroMol.Modules.Identity.Application.Users.ForgotPassword;
using LabViroMol.Modules.Identity.Application.Users.Login;
using LabViroMol.Modules.Identity.Application.Users.Logout;
using LabViroMol.Modules.Identity.Application.Users.ReactivateUser;
using LabViroMol.Modules.Identity.Application.Users.RefreshToken;
using LabViroMol.Modules.Identity.Application.Users.ResetPassword;
using LabViroMol.Modules.Identity.Application.Users.UpdateProfile;
using LabViroMol.Modules.Identity.Application.Users.UpdateUser;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Infrastructure.Users;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LabViroMol.Modules.Identity.Presentation.Users;

public record UpdateUserRequest(UserInfo UserData, List<Guid> RoleIds);
public record UpdateProfileRequest(UserInfo UserData);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

internal static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users");

        group.MapGet("/", async ([AsParameters] PagedRequest request, UserQueries queries) =>
            Results.Ok(await queries.GetAllAsync(request)))
            .RequireAuthorization(Permissions.Identity.UsersView);

        group.MapGet("/me", async (ICurrentUser currentUser, UserQueries queries, CancellationToken ct) =>
        {
            var user = await queries.GetByIdAsync(currentUser.Id.Value, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, UserQueries queries, CancellationToken ct) =>
        {
            var user = await queries.GetByIdAsync(id, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        }).RequireAuthorization(Permissions.Identity.UsersView);

        group.MapPost("/", async (CreateUserCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Created("/api/identity/users", (object?)null));
        }).RequireAuthorization()
          .Accepts<CreateUserCommand>("application/json");

        group.MapPut("/me", async (UpdateProfileRequest request, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateProfileCommand(currentUser.Id.Value, request.UserData);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).RequireAuthorization()
          .Accepts<UpdateProfileRequest>("application/json");

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateUserCommand(id, request.UserData, request.RoleIds);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).RequireAuthorization(Permissions.Identity.UsersManage)
          .Accepts<UpdateUserRequest>("application/json");

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new DeactivateUserCommand(id);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).RequireAuthorization(Permissions.Identity.UsersManage);

        group.MapPost("/{id:guid}/reactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ReactivateUserCommand(id);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).RequireAuthorization(Permissions.Identity.UsersManage);

        group.MapPost("/change-password", async (ChangePasswordRequest request, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var command = new ChangePasswordCommand(currentUser.Id.Value, request.CurrentPassword, request.NewPassword);
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).RequireAuthorization()
          .Accepts<ChangePasswordRequest>("application/json");

        group.MapPost("/login", async (LoginCommand command, IMediator mediator, HttpContext httpContext, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            if (result.IsFailure)
                return result.ToHttpResult(Results.Ok());

            var (accessToken, refreshToken) = result.Data!;

            httpContext.Response.Cookies.Append("X-Access-Token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(2)
            });

            httpContext.Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/identity/users/refresh",
                MaxAge = TimeSpan.FromDays(7)
            });

            return Results.Ok();
        }).Accepts<LoginCommand>("application/json");

        group.MapPost("/refresh", async (IMediator mediator, HttpContext httpContext, CancellationToken ct) =>
        {
            if (!httpContext.Request.Cookies.TryGetValue("X-Refresh-Token", out var refreshToken))
                return Results.BadRequest(new { Errors = new[] { "Token de atualização não encontrado." } });

            var command = new RefreshTokenCommand(refreshToken);
            var result = await mediator.Send(command, ct);

            if (result.IsFailure)
                return result.ToHttpResult(Results.Ok());

            var (newAccessToken, newRefreshToken) = result.Data!;

            httpContext.Response.Cookies.Append("X-Access-Token", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(2)
            });

            httpContext.Response.Cookies.Append("X-Refresh-Token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/identity/users/refresh",
                MaxAge = TimeSpan.FromDays(7)
            });

            return Results.Ok();
        });

        group.MapPost("/logout", async (ICurrentUser currentUser, IMediator mediator, HttpContext httpContext, CancellationToken ct) =>
        {
            var command = new LogoutCommand(currentUser.Id.Value);
            await mediator.Send(command, ct);

            httpContext.Response.Cookies.Delete("X-Access-Token");
            httpContext.Response.Cookies.Delete("X-Refresh-Token", new CookieOptions
            {
                Path = "/api/identity/users/refresh"
            });

            return Results.Ok();
        }).RequireAuthorization();

        group.MapPost("/forgot-password", async (ForgotPasswordCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).Accepts<ForgotPasswordCommand>("application/json");

        group.MapPost("/reset-password", async (ResetPasswordCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            return result.ToHttpResult(Results.Ok());
        }).Accepts<ResetPasswordCommand>("application/json");
    }
}
