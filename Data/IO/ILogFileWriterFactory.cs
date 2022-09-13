namespace QuickTrack.Data.IO;

public interface ILogFileWriterFactory
{
    ILogFileWriter Create(string fileName);
}