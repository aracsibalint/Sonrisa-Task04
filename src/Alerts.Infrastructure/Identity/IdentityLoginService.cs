using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alerts.Infrastructure.Identity;

public sealed class IdentityLoginService(
    IServiceScopeFactory scopeFactory,
    IMutationGate gate,
    IPasswordHasher<IdentityUser> hasher) : IIdentityLogin
{
    private readonly IdentityUser dummyUser = new() { Id = Guid.NewGuid().ToString("D"), UserName = "dummy", NormalizedUserName = "DUMMY" };
    private string? dummyHash;

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        // Bounded above the per-IP limiter's maximum concurrent attempts so each admitted
        // request can re-verify after every preceding serialized security-state change.
        for (var attempt = 0; attempt < 11; attempt++)
        {
            IdentitySnapshot? snapshot;
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
                snapshot = await db.Users.AsNoTracking().Where(x => x.NormalizedEmail == normalizedEmail)
                    .Select(x => new IdentitySnapshot(x.Id, x.Email!, x.PasswordHash!, x.SecurityStamp!, x.ConcurrencyStamp!, x.AccessFailedCount, x.LockoutEnd))
                    .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }

            var verification = snapshot is null
                ? hasher.VerifyHashedPassword(dummyUser, dummyHash ??= hasher.HashPassword(dummyUser, Guid.NewGuid().ToString("D")), password)
                : hasher.VerifyHashedPassword(new IdentityUser { Id = snapshot.Id }, snapshot.PasswordHash, password);
            var valid = snapshot is not null && verification is not PasswordVerificationResult.Failed;
            var replacementHash = valid && verification == PasswordVerificationResult.SuccessRehashNeeded
                ? hasher.HashPassword(new IdentityUser { Id = snapshot!.Id }, password)
                : null;

            var result = await gate.ExecuteAsync(async (admission, ct) =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var unit = scope.ServiceProvider.GetRequiredService<PersistenceUnitOfWork>();
                var purpose = replacementHash is null ? MutationPurpose.IdentitySecurityBookkeeping : MutationPurpose.Normal;
                return await unit.ExecuteAsync(admission, purpose, async (db, token) =>
                {
                    var user = snapshot is null ? null : await db.Users.SingleOrDefaultAsync(x => x.Id == snapshot.Id, token).ConfigureAwait(false);
                    if (snapshot is not null && (user is null || user.PasswordHash != snapshot.PasswordHash
                        || user.SecurityStamp != snapshot.SecurityStamp || user.ConcurrencyStamp != snapshot.ConcurrencyStamp
                        || user.AccessFailedCount != snapshot.AccessFailedCount || user.LockoutEnd != snapshot.LockoutEnd))
                        return new LoginResult(LoginOutcome.Retry);

                    if (user is null)
                        return new LoginResult(LoginOutcome.InvalidCredentials);

                    if (!user.LockoutEnabled || user.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)
                        return new LoginResult(LoginOutcome.InvalidCredentials);

                    if (!valid)
                    {
                        user.AccessFailedCount++;
                        if (user.AccessFailedCount >= 5)
                        {
                            user.AccessFailedCount = 0;
                            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            user.LockoutEnd = DateTimeOffset.FromUnixTimeMilliseconds(checked(nowMs + 900_000));
                        }
                        user.ConcurrencyStamp = Guid.NewGuid().ToString("D");
                        return new LoginResult(LoginOutcome.InvalidCredentials);
                    }

                    if (user.AccessFailedCount != 0 || user.LockoutEnd is not null)
                    {
                        user.AccessFailedCount = 0;
                        user.LockoutEnd = null;
                        user.ConcurrencyStamp = Guid.NewGuid().ToString("D");
                    }
                    if (replacementHash is not null)
                    {
                        user.PasswordHash = replacementHash;
                        user.SecurityStamp = Guid.NewGuid().ToString("D");
                        user.ConcurrencyStamp = Guid.NewGuid().ToString("D");
                    }

                    var roles = await (from ur in db.UserRoles
                                       join role in db.Roles on ur.RoleId equals role.Id
                                       where ur.UserId == user.Id
                                       orderby role.Name
                                       select role.Name!).ToListAsync(token).ConfigureAwait(false);
                    return new LoginResult(LoginOutcome.Success, new AuthenticatedAccount(user.Id, user.Email!, roles));
                }, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            if (result.Outcome != LoginOutcome.Retry) return result;
        }
        return new LoginResult(LoginOutcome.InvalidCredentials);
    }

    private sealed record IdentitySnapshot(string Id, string Email, string PasswordHash, string SecurityStamp,
        string ConcurrencyStamp, int AccessFailedCount, DateTimeOffset? LockoutEnd);
}
