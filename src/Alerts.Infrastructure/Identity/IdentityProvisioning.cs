using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alerts.Infrastructure.Identity;

public sealed record ProvisionedAccount(string Id, string Email, string Password, string Role);

public sealed class IdentityProvisioningOptions
{
    public List<ProvisionedAccount> Accounts { get; init; } = [];
}

public sealed class IdentityProvisioner(
    IServiceScopeFactory scopeFactory,
    IMutationGate gate,
    IPasswordHasher<IdentityUser> hasher,
    IOptions<IdentityOptions> identityOptions,
    IIdentityProvisioningFault fault)
{
    private static readonly DateTimeOffset ProvisioningDisabledUntil =
        DateTimeOffset.FromUnixTimeMilliseconds(253_402_300_799_999);

    public async Task ProvisionAsync(IReadOnlyList<ProvisionedAccount> accounts, CancellationToken cancellationToken = default)
    {
        if (accounts.Count == 0)
            throw new InvalidOperationException("Identity provisioning requires explicitly configured accounts.");

        var validated = new List<(ProvisionedAccount Account, string NormalizedEmail, IdentityUser User)>();
        foreach (var account in accounts)
        {
            ValidateAccount(account);
            var user = NewUser(account, account.Email.ToUpperInvariant());
            ValidatePassword(user, account.Password, identityOptions.Value.Password);
            validated.Add((account, user.NormalizedEmail!, user));
        }

        if (validated.Select(x => x.Account.Id).Distinct(StringComparer.Ordinal).Count() != validated.Count
            || validated.Select(x => x.NormalizedEmail).Distinct(StringComparer.Ordinal).Count() != validated.Count)
            throw new InvalidOperationException("Provisioned account IDs and normalized emails must be unique.");
        if (!validated.Any(x => x.Account.Role == "Admin"))
            throw new InvalidOperationException("Identity provisioning requires at least one configured Admin account.");

        // Hash only after the complete declarative input is known to be unambiguous. Hashing stays
        // outside admission even though hashes are used only for genuinely new accounts.
        var prepared = validated.Select(x => (x.Account, x.NormalizedEmail,
            PasswordHash: hasher.HashPassword(x.User, x.Account.Password))).ToArray();

        await gate.ExecuteAsync(async (admission, ct) =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var unit = scope.ServiceProvider.GetRequiredService<PersistenceUnitOfWork>();
            return await unit.ExecuteAsync(admission, MutationPurpose.IdentityProvisioning, async (db, token) =>
            {
                var roles = await db.Roles.ToDictionaryAsync(x => x.NormalizedName!, StringComparer.Ordinal, token).ConfigureAwait(false);
                if (!roles.TryGetValue("USER", out var userRole) || userRole.Id != StableIdentityIds.UserRoleId
                    || !roles.TryGetValue("ADMIN", out var adminRole) || adminRole.Id != StableIdentityIds.AdminRoleId)
                    throw new InvalidOperationException("Stable Identity roles are missing or conflicting.");

                var configuredIds = prepared.Select(x => x.Account.Id).ToHashSet(StringComparer.Ordinal);
                var persistedUsers = await db.Users.ToListAsync(token).ConfigureAwait(false);
                var memberships = await db.UserRoles.ToListAsync(token).ConfigureAwait(false);
                foreach (var item in prepared)
                {
                    var byId = persistedUsers.SingleOrDefault(x => x.Id == item.Account.Id);
                    var byEmail = persistedUsers.SingleOrDefault(x => x.NormalizedEmail == item.NormalizedEmail);
                    if (byId is not null && (byId.NormalizedEmail != item.NormalizedEmail || byId.Email != item.Account.Email
                            || byId.NormalizedUserName != item.NormalizedEmail || byId.UserName != item.Account.Email)
                        || byEmail is not null && byEmail.Id != item.Account.Id)
                        throw new InvalidOperationException("A provisioned account identity conflicts with persisted Identity data.");

                    var user = byId;
                    if (user is null)
                    {
                        user = NewUser(item.Account, item.NormalizedEmail);
                        user.PasswordHash = item.PasswordHash;
                        db.Users.Add(user);
                        persistedUsers.Add(user);
                    }
                    else if (!user.LockoutEnabled)
                    {
                        user.LockoutEnabled = true;
                        if (user.LockoutEnd == ProvisioningDisabledUntil)
                        {
                            user.LockoutEnd = null;
                            user.AccessFailedCount = 0;
                        }
                        RefreshStamps(user);
                    }

                    var requiredRoleIds = (item.Account.Role == "Admin"
                        ? new[] { userRole.Id, adminRole.Id }
                        : new[] { userRole.Id }).ToHashSet(StringComparer.Ordinal);
                    var existing = memberships.Where(x => x.UserId == user.Id).ToArray();
                    var removed = existing.Where(x => !requiredRoleIds.Contains(x.RoleId)).ToArray();
                    if (removed.Length != 0) db.UserRoles.RemoveRange(removed);
                    foreach (var roleId in requiredRoleIds.Except(existing.Select(x => x.RoleId), StringComparer.Ordinal))
                    {
                        var membership = new IdentityUserRole<string> { UserId = user.Id, RoleId = roleId };
                        db.UserRoles.Add(membership);
                        memberships.Add(membership);
                    }
                    if (removed.Length != 0 || requiredRoleIds.Except(existing.Select(x => x.RoleId), StringComparer.Ordinal).Any())
                        RefreshStamps(user);
                }

                foreach (var user in persistedUsers.Where(x => !configuredIds.Contains(x.Id)))
                {
                    var staleMemberships = memberships.Where(x => x.UserId == user.Id).ToArray();
                    if (staleMemberships.Length != 0) db.UserRoles.RemoveRange(staleMemberships);
                    if (user.LockoutEnabled || user.LockoutEnd != ProvisioningDisabledUntil || user.AccessFailedCount != 0)
                    {
                        user.LockoutEnabled = false;
                        user.LockoutEnd = ProvisioningDisabledUntil;
                        user.AccessFailedCount = 0;
                        RefreshStamps(user);
                    }
                }
                fault.BeforeCommit();
                return true;
            }, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void RefreshStamps(IdentityUser user)
    {
        user.SecurityStamp = Guid.NewGuid().ToString("D");
        user.ConcurrencyStamp = Guid.NewGuid().ToString("D");
    }

    private static IdentityUser NewUser(ProvisionedAccount account, string normalizedEmail) => new()
    {
        Id = account.Id,
        UserName = account.Email,
        NormalizedUserName = normalizedEmail,
        Email = account.Email,
        NormalizedEmail = normalizedEmail,
        EmailConfirmed = true,
        LockoutEnabled = true,
        SecurityStamp = Guid.NewGuid().ToString("D"),
        ConcurrencyStamp = Guid.NewGuid().ToString("D")
    };

    private static void ValidateAccount(ProvisionedAccount account)
    {
        if (!Guid.TryParseExact(account.Id, "D", out var id) || id == Guid.Empty || id.ToString("D") != account.Id)
            throw new InvalidOperationException("Provisioned account IDs must be canonical nonempty lowercase GUIDs.");
        if (string.IsNullOrWhiteSpace(account.Email) || account.Email.Length > 256 || account.Email != account.Email.Trim()
            || !account.Email.Contains('@', StringComparison.Ordinal))
            throw new InvalidOperationException("Provisioned account email is invalid.");
        if (account.Role is not ("User" or "Admin"))
            throw new InvalidOperationException("Provisioned account role must be User or Admin.");
    }

    private static void ValidatePassword(IdentityUser user, string password, PasswordOptions options)
    {
        if (string.IsNullOrEmpty(password) || password.Length is < 14 or > 128
            || options.RequireDigit && !password.Any(char.IsDigit)
            || options.RequireLowercase && !password.Any(char.IsLower)
            || options.RequireUppercase && !password.Any(char.IsUpper)
            || options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit)
            || password.Distinct().Count() < options.RequiredUniqueChars)
            throw new InvalidOperationException($"Provisioned password for account {user.Id} does not satisfy the configured Identity policy.");
    }
}

public interface IIdentityProvisioningFault
{
    void BeforeCommit();
}

public sealed class NoIdentityProvisioningFault : IIdentityProvisioningFault
{
    public void BeforeCommit() { }
}

public static class StableIdentityIds
{
    public const string UserRoleId = "00000000-0000-0000-0000-000000000001";
    public const string AdminRoleId = "00000000-0000-0000-0000-000000000002";
}
