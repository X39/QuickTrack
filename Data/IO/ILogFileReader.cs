namespace QuickTrack.Data.IO;

public interface ILogFileReader : IAsyncDisposable
{
    /// <summary>
    /// Parses at least enough data to read the next line.
    /// </summary>
    /// <param name="cancellationToken">Allows to cancel the read operation.</param>
    /// <returns>Returns the next line in the file or null if there are no more lines.</returns>
    ValueTask<TimeLogLine?> ReadLineAsync(CancellationToken cancellationToken);
}