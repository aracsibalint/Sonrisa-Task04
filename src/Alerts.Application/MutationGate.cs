namespace Alerts.Application;

public enum MutationPurpose { Normal, SendResultOrRecovery, IdentitySecurityBookkeeping, IdentityProvisioning }

// A capability passed by the outer use case, never acquired by a scoped participant.
public sealed class MutationAdmission
{
    internal MutationAdmission() { }
    public bool IsActive { get; internal set; } = true;
    public void RequireActive()
    {
        if (!IsActive) throw new InvalidOperationException("Mutation admission has ended.");
    }
}

public interface IMutationGate
{
    Task<T> ExecuteAsync<T>(Func<MutationAdmission, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}

public sealed class WriterBusyException : Exception;

public sealed class MutationGate : IMutationGate, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly AsyncLocal<bool> entered = new();
    private readonly TimeSpan deadline;
    private int waiters;

    public MutationGate() : this(TimeSpan.FromSeconds(2)) { }
    public MutationGate(TimeSpan deadline) => this.deadline = deadline;

    public async Task<T> ExecuteAsync<T>(Func<MutationAdmission, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (entered.Value) throw new InvalidOperationException("Mutation gate is nonreentrant.");
        if (Interlocked.Increment(ref waiters) > 32)
        {
            Interlocked.Decrement(ref waiters);
            throw new WriterBusyException();
        }
        bool acquired;
        try { acquired = await semaphore.WaitAsync(deadline, cancellationToken).ConfigureAwait(false); }
        finally { Interlocked.Decrement(ref waiters); }
        if (!acquired) throw new WriterBusyException();
        entered.Value = true;
        var admission = new MutationAdmission();
        try { return await operation(admission, cancellationToken).ConfigureAwait(false); }
        finally
        {
            admission.IsActive = false;
            entered.Value = false;
            semaphore.Release();
        }
    }

    public void Dispose() => semaphore.Dispose();
}
