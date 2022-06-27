using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace QuickTrack;

public class TimeLogFile
{
    public string FilePath { get; }
    public DateOnly Date { get; }

    public const string FileExtensionV1 = ".tlog";
    public const string FileExtensionV2 = ".tlog2";

    public static readonly ImmutableArray<string> Extensions = new[]
    {
        FileExtensionV1,
        FileExtensionV2,
    }.ToImmutableArray();

    public enum EFileVersion
    {
        V1,
        V2,
    }

    public EFileVersion FileVersion { get; }

    public TimeLogFile(string filePath, DateOnly date)
    {
        static EFileVersion SetVersion(EFileVersion version, string inFilePath, out string outFilePath)
        {
            var file = Path.GetFileNameWithoutExtension(inFilePath);
            outFilePath = version switch
            {
                EFileVersion.V1 => Path.ChangeExtension(file, FileExtensionV1),
                EFileVersion.V2 => Path.ChangeExtension(file, FileExtensionV2),
                _               => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };
            return version;
        }
        var ext = Path.GetExtension(filePath);
        FileVersion = ext switch
        {
            FileExtensionV1 => SetVersion(EFileVersion.V1, filePath, out filePath),
            FileExtensionV2 => SetVersion(EFileVersion.V2, filePath, out filePath),
            _               => SetVersion(EFileVersion.V1, filePath, out filePath)
        };

        FilePath = filePath;
        Date = date;
    }

    public IEnumerable<TimeLogLine> GetLines()
    {
        if (!File.Exists(FilePath))
            return Enumerable.Empty<TimeLogLine>();
        switch (FileVersion)
        {
            case EFileVersion.V1:
                return GetLinesOfTLogV1();
            case EFileVersion.V2:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Append(TimeLogLine timeLogLine)
    {
        switch (FileVersion)
        {
            case EFileVersion.V1:
                AppendToTLogV1(timeLogLine);
                break;
            case EFileVersion.V2:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Clear()
    {
        using var file = File.Open(FilePath, FileMode.Create);
    }

    public void OpenWithDefaultProgram()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = FilePath,
            UseShellExecute = true,
        };
        var process = Process.Start(processStartInfo);
    }

    private IEnumerable<TimeLogLine> GetLinesOfTLogV1()
    {
        var lines = File.ReadAllLines(FilePath);
        var last = new TimeLogLine(default, default, string.Empty, string.Empty);
        foreach (var line in lines)
        {
            var splatted = line.Split("|!|");
            if (splatted.Length < 2)
                continue;
            var tmp = new TimeLogLine(
                DateTime.ParseExact(
                    splatted[0],
                    "s",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal),
                default,
                splatted[1],
                string.Join("|!|", splatted.Skip(2)));
            if (last.TimeStampStart != default)
            {
                yield return last with {TimeStampEnd = tmp.TimeStampStart};
            }

            last = tmp;
        }

        if (last.TimeStampStart != default)
        {
            yield return last;
        }
    }

    private void AppendToTLogV1(TimeLogLine timeLogLine)
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
        var timeStampString = timeLogLine.TimeStampStart
            .ToUniversalTime()
            .ToString("s", CultureInfo.InvariantCulture);
        writer.WriteLine(
            $"{timeStampString}|!|{timeLogLine.Project}|!|{timeLogLine.Message}");
    }
}