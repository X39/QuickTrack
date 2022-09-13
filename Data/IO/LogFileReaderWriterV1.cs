namespace QuickTrack.Data.IO;

public class LogFileReaderV1 : ILogFileReader
{
    public async ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
    
    public async ValueTask<TimeLogLine> ReadLineAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}