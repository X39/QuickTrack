namespace QuickTrack.Data.IO;

public interface ILogFileWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes the specified log entry to the log file.
    /// </summary>
    /// <param name="timeLogLine">The log-line</param>
    /// <param name="cancellationToken">Allows to cancel the write operation.</param>
    /// <returns>An awaitable <see cref="ValueTask"/></returns>
    ValueTask WriteLineAsync(TimeLogLine timeLogLine, CancellationToken cancellationToken);
}