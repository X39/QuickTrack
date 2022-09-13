namespace QuickTrack.Data.IO;

public interface ILogFileReaderFactory
{
    ILogFileReader CreateReader(string filePath);
}