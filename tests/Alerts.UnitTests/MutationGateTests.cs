using Alerts.Application;
using Xunit;
using Alerts.Infrastructure.Persistence;

namespace Alerts.UnitTests;

public sealed class MutationGateTests
{
    [Fact]
    public async Task ConcurrentMutations_Serialize_AndInvalidateAdmission()
    {
        using var gate = new MutationGate();
        var active = 0;
        MutationAdmission? captured = null;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => gate.ExecuteAsync(async (a, ct) =>
        {
            Assert.Equal(1, Interlocked.Increment(ref active));
            captured = a;
            await Task.Yield();
            Interlocked.Decrement(ref active);
            return true;
        }, TestContext.Current.CancellationToken)));
        Assert.Throws<InvalidOperationException>(() => captured!.RequireActive());
    }

    [Fact]
    public async Task CancellationTimeoutAndException_DoNotReleaseAnotherOwner_AndGateRecovers()
    {
        using var gate = new MutationGate(TimeSpan.FromMilliseconds(50));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = gate.ExecuteAsync(async (_, _) => { started.SetResult(); await release.Task; return true; }, TestContext.Current.CancellationToken);
        await started.Task;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.ExecuteAsync((_, _) => Task.FromResult(true), cancelled.Token));
        await Assert.ThrowsAsync<WriterBusyException>(() => gate.ExecuteAsync((_, _) => Task.FromResult(true), TestContext.Current.CancellationToken));
        Assert.False(owner.IsCompleted);
        release.SetResult();
        await owner;
        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.ExecuteAsync<bool>((_, _) => throw new InvalidOperationException(), TestContext.Current.CancellationToken));
        Assert.True(await gate.ExecuteAsync((_, _) => Task.FromResult(true), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoreThan32Waiters_IsRejected_WithoutStealingOwnership()
    {
        using var gate = new MutationGate();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = gate.ExecuteAsync(async (_, _) => { started.SetResult(); await release.Task; return true; }, TestContext.Current.CancellationToken);
        await started.Task;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var waiters = Enumerable.Range(0, 32).Select(_ => gate.ExecuteAsync((_, _) => Task.FromResult(true), cancel.Token)).ToArray();
        try
        {
            await Assert.ThrowsAsync<WriterBusyException>(() => gate.ExecuteAsync((_, _) => Task.FromResult(true), TestContext.Current.CancellationToken));
            Assert.False(owner.IsCompleted);
            cancel.Cancel();
            foreach (var waiter in waiters) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
        }
        finally { release.TrySetResult(); await owner; }
    }

    [Fact]
    public async Task NestedAcquisition_FailsImmediately_WithoutDeadlock()
    {
        using var gate = new MutationGate();
        await gate.ExecuteAsync(async (admission, ct) =>
        {
            admission.RequireActive();
            await Assert.ThrowsAsync<InvalidOperationException>(() => gate.ExecuteAsync((_, _) => Task.FromResult(true), ct));
            return true;
        }, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(MutationPurpose.Normal, 1090519040L, true)]
    [InlineData(MutationPurpose.Normal, 1090519039L, false)]
    [InlineData(MutationPurpose.IdentityProvisioning, 1090519040L, true)]
    [InlineData(MutationPurpose.IdentityProvisioning, 1090519039L, false)]
    [InlineData(MutationPurpose.SendResultOrRecovery, 269484032L, true)]
    [InlineData(MutationPurpose.IdentitySecurityBookkeeping, 269484031L, false)]
    public void StoragePolicy_UsesExactAllowanceAndThreshold(MutationPurpose purpose, long free, bool allowed)
    {
        var policy = new StorageAdmission("unused", new FakeSpace(free));
        if (allowed) policy.Check(purpose);
        else Assert.Throws<StorageUnavailableException>(() => policy.Check(purpose));
    }

    [Fact]
    public void StorageFault_AndUnmeasurableVolume_FailClosed()
    {
        var fault = new StorageAdmission("unused", new FakeSpace(long.MaxValue));
        fault.MarkFaulted();
        Assert.Throws<StorageUnavailableException>(() => fault.Check(MutationPurpose.Normal));
        var unknown = new StorageAdmission("unused", new UnknownSpace());
        Assert.Throws<StorageUnavailableException>(() => unknown.Check(MutationPurpose.SendResultOrRecovery));
        Assert.True(unknown.IsFaulted);
    }

    private sealed class FakeSpace(long bytes) : IFreeSpaceProbe { public long AvailableBytes(string _) => bytes; }
    private sealed class UnknownSpace : IFreeSpaceProbe { public long AvailableBytes(string _) => throw new IOException(); }
}
