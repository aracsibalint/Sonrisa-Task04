using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Alerts.Infrastructure.Persistence;

// Cooperative local sandbox ownership, NOT protection from a hostile local operator.
public sealed class WindowsDatabaseOwner : IDisposable
{
    private readonly FileStream sidecar;
    private readonly FileStream database;
    private bool disposed;
    public string DatabasePath { get; }

    private WindowsDatabaseOwner(string path, FileStream sidecar, FileStream database)
        => (DatabasePath, this.sidecar, this.database) = (path, sidecar, database);

    public static WindowsDatabaseOwner Acquire(string path, bool allowCreate = false)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows local NTFS is required.");
        if (!Path.IsPathFullyQualified(path) || path.Length < 4 || path[1] != ':' || path[2] != '\\'
            || path[3..].Contains(':') || path.Contains('/') || path.StartsWith("\\", StringComparison.Ordinal)
            || path.Split('\\').Skip(1).Any(s => s.Length == 0 || s is "." or ".." || s.EndsWith('.') || s.EndsWith(' ')))
            throw new IOException("Unsupported database path.");
        var full = Path.GetFullPath(path);
        var drive = new DriveInfo(Path.GetPathRoot(full)!);
        if (drive.DriveType != DriveType.Fixed || !string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
            throw new IOException("A fixed local NTFS volume is required.");
        ValidateTraversal(full);
        ValidateTraversal(full + ".writer.lock");
        FileStream? sidecar = null;
        FileStream? database = null;
        try
        {
            sidecar = new FileStream(full + ".writer.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Verify(sidecar.SafeFileHandle, full + ".writer.lock");
            // Keep a non-delete-sharing DB handle: ordinary replacement is refused for this lifetime.
            database = new FileStream(full, allowCreate ? FileMode.OpenOrCreate : FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            Verify(database.SafeFileHandle, full);
            ValidateTraversal(full);
            var owner = new WindowsDatabaseOwner(full, sidecar, database);
            owner.RequireActive();
            return owner;
        }
        catch { database?.Dispose(); sidecar?.Dispose(); throw; }
    }

    public void RequireActive()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Verify(database.SafeFileHandle, DatabasePath);
        Verify(sidecar.SafeFileHandle, DatabasePath + ".writer.lock");
        ValidateTraversal(DatabasePath);
        foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
        {
            var auxiliary = DatabasePath + suffix;
            ValidateTraversal(auxiliary);
            if (!File.Exists(auxiliary)) continue;
            using var stream = new FileStream(auxiliary, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Verify(stream.SafeFileHandle, auxiliary);
        }
    }

    private static void ValidateTraversal(string path)
    {
        var current = Path.GetPathRoot(path)!;
        foreach (var part in path[current.Length..].Split('\\'))
        {
            current = Path.Combine(current, part);
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Reparse traversal is unsupported.");
            }
            catch (FileNotFoundException) when (current == path) { }
        }
    }

    private static void Verify(SafeFileHandle handle, string expected)
    {
        var text = new StringBuilder(32768);
        var length = GetFinalPathNameByHandle(handle, text, (uint)text.Capacity, 0);
        if (length == 0 || length >= text.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error());
        if (!string.Equals(text.ToString(), "\\\\?\\" + expected, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Unsupported path alias/final path.");
        if (!GetFileInformationByHandle(handle, out var info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        if (info.NumberOfLinks != 1 || (info.Attributes & (uint)FileAttributes.ReparsePoint) != 0)
            throw new IOException("Hardlinks/reparse files are unsupported.");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        database.Dispose();
        sidecar.Dispose(); // Deliberately leave the sidecar. File existence is not ownership.
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime, LastAccessTime, LastWriteTime;
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle file, StringBuilder path, uint size, uint flags);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInformation information);
}
