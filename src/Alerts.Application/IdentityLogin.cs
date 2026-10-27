namespace Alerts.Application;

public sealed record AuthenticatedAccount(string Id, string Email, IReadOnlyList<string> Roles);

public enum LoginOutcome { Success, InvalidCredentials, Retry }

public sealed record LoginResult(LoginOutcome Outcome, AuthenticatedAccount? Account = null);

public interface IIdentityLogin
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
