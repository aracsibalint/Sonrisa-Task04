using System.Security.Claims;
using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Alerts.Application.Rules;

namespace Alerts.Api.Authentication;

public sealed record LoginRequest(string? Email, string? Password);

public static class AuthenticationEndpoints
{
    public static void MapAlertsIdentity(this WebApplication app, bool persistenceConfigured)
    {
        if (!persistenceConfigured)
        {
            app.MapPost("/api/v1/auth/login", () => Unavailable()).AllowAnonymous().RequireRateLimiting("login");
        }
        else
        {
            app.MapPost("/api/v1/auth/login", async (LoginRequest request, HttpContext http, IIdentityLogin login, CancellationToken ct) =>
            {
                if (string.IsNullOrEmpty(request.Email) || request.Email.Length > 256
                    || string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
                    return InvalidCredentials();

                LoginResult result;
                try { result = await login.LoginAsync(request.Email, request.Password, ct).ConfigureAwait(false); }
                catch (WriterBusyException) { return Unavailable(); }
                catch (StorageUnavailableException) { return Unavailable(); }
                if (result.Outcome != LoginOutcome.Success || result.Account is null) return InvalidCredentials();

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, result.Account.Id),
                    new(ClaimTypes.Name, result.Account.Email),
                    new(ClaimTypes.Email, result.Account.Email)
                };
                claims.AddRange(result.Account.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.BearerScheme));
                await http.SignInAsync(IdentityConstants.BearerScheme, principal).ConfigureAwait(false);
                return Results.Empty;
            }).AllowAnonymous().RequireRateLimiting("login");
        }

        app.MapGet("/api/v1/me", (ClaimsPrincipal principal) =>
        {
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var email = principal.FindFirstValue(ClaimTypes.Email)!;
            var roles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).Order(StringComparer.Ordinal).ToArray();
            return Results.Ok(new { id, email, roles });
        }).RequireAuthorization("Owner");

        app.MapGet("/api/v1/me/channels", async (ClaimsPrincipal principal, [FromServices] RuleUseCases rules, CancellationToken ct) =>
        {
            var ownerId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var available = await rules.ChannelsAsync(ownerId, ct).ConfigureAwait(false);
            var channels = available.Select(x => new { channel = x.Channel.ToLowerInvariant(), x.Available, x.Reason });
            return Results.Ok(new { channels });
        }).RequireAuthorization("Owner");
    }

    private static IResult InvalidCredentials() => Results.Json(new
    {
        type = "urn:alerts:problem:invalid-credentials",
        title = "Invalid credentials.",
        status = 401,
        detail = "The supplied credentials are invalid.",
        code = "InvalidCredentials"
    }, statusCode: StatusCodes.Status401Unauthorized, contentType: "application/problem+json");

    private static IResult Unavailable() => Results.Json(new
    {
        type = "urn:alerts:problem:storage-unavailable",
        title = "The service is temporarily unavailable.",
        status = 503,
        detail = "Retry later.",
        code = "StorageUnavailable"
    }, statusCode: StatusCodes.Status503ServiceUnavailable, contentType: "application/problem+json");
}
