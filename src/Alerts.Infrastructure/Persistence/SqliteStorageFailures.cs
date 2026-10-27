using Microsoft.Data.Sqlite;

namespace Alerts.Infrastructure.Persistence;

internal static class SqliteStorageFailures
{
    // SQLite extended result codes retain their primary category in the low byte.
    // Only operational storage availability categories are transient503/latch-worthy.
    internal static bool IsStorageAvailability(SqliteException exception)
    {
        var reported = exception.SqliteExtendedErrorCode != 0
            ? exception.SqliteExtendedErrorCode
            : exception.SqliteErrorCode;
        return (reported & 0xff) is 5   // SQLITE_BUSY
            or 6                        // SQLITE_LOCKED
            or 8                        // SQLITE_READONLY
            or 10                       // SQLITE_IOERR
            or 13                       // SQLITE_FULL
            or 14;                      // SQLITE_CANTOPEN
    }
}
