using System.Globalization;

namespace QuickTrack;

public class TimeLogFile
{
    public string FilePath { get; }
    public DateOnly Date { get; }

    public TimeLogFile(string filePath, DateOnly date)
    {
        if (Path.GetExtension(filePath) != ".tlog")
        {
            filePath = Path.ChangeExtension(filePath, ".tlog");
        }

        FilePath = filePath;
        Date = date;
    }

    public IEnumerable<TimeLogLine> GetLines()
    {
        if (!File.Exists(FilePath))
            yield break;
        var lines = File.ReadAllLines(FilePath);
        var last = new TimeLogLine(default, default, string.Empty, string.Empty);
        foreach (var line in lines)
        {
            var splatted = line.Split("|!|");
            if (splatted.Length >= 2)
            {
                var tmp = new TimeLogLine(
                    DateTime.ParseExact(splatted[0], "s", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    default,
                    splatted[1],
                    string.Join("|!|", splatted.Skip(2)));
                if (last.TimeStampStart != default)
                {
                    yield return last with {TimeStampEnd = tmp.TimeStampStart};
                }

                last = tmp;
            }
        }

        if (last.TimeStampStart != default)
        {
            yield return last;
        }
    }

    public void Append(TimeLogLine timeLogLine)
    {
        timeLogLine = timeLogLine with
        {
            Project = timeLogLine.Project.Trim(),
            Message = string.Concat(timeLogLine.Message.Trim().TrimEnd('.'), "."),
        };
        using var file = File.Exists(FilePath)
            ? File.Open(FilePath, FileMode.Append)
            : File.Open(FilePath, FileMode.CreateNew);
        using var writer = new StreamWriter(file);
        writer.WriteLine(
            $"{timeLogLine.TimeStampStart.ToString("s", CultureInfo.InvariantCulture)}|!|{timeLogLine.Project}|!|{timeLogLine.Message}");
    }
}