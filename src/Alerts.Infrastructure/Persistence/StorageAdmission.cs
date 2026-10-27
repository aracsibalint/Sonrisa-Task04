using Alerts.Application;

namespace Alerts.Infrastructure.Persistence;

public interface IFreeSpaceProbe
{
    long AvailableBytes(string databasePath);
}

public sealed class VolumeFreeSpaceProbe : IFreeSpaceProbe
{
    public long AvailableBytes(string databasePath) => new DriveInfo(Path.GetPathRoot(databasePath)!).AvailableFreeSpace;
}

public sealed class StorageUnavailableException : IOException
{
    public StorageUnavailableException() : base("Storage admission is unavailable.") { }
}

public sealed class StorageAdmission(string databasePath, IFreeSpaceProbe probe)
{
    private int faulted;
    public bool IsFaulted => Volatile.Read(ref faulted) != 0;
    public void MarkFaulted() => Interlocked.Exchange(ref faulted, 1);
    public void Check(MutationPurpose purpose)
    {
        if (!Enum.IsDefined(purpose)) throw new ArgumentOutOfRangeException(nameof(purpose));
        if (IsFaulted) throw new StorageUnavailableException();
        long bytes;
        try { bytes = probe.AvailableBytes(databasePath); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        { MarkFaulted(); throw new StorageUnavailableException(); }
        var emergency = purpose is MutationPurpose.SendResultOrRecovery or MutationPurpose.IdentitySecurityBookkeeping;
        var required = !emergency ? (1L << 30) + (16L << 20) : (256L << 20) + (1L << 20);
        if (bytes < required) throw new StorageUnavailableException();
    }
}
