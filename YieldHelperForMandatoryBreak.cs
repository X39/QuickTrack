#pragma warning disable CA2211
using JetBrains.Annotations;
using X39.Util.Console;
using X39.Util;

namespace QuickTrack;

[PublicAPI]
public class YieldHelperForMandatoryBreak
{
    private record struct InsertPosition(int TotalLines, int LineIndex);
    private readonly ConfigHost _configHost;
    public ConsoleColor ListingBackgroundColor { get; set; } = ConsoleColor.DarkCyan;
    public ConsoleColor ValidRangeForegroundColor { get; set; } = ConsoleColor.White;
    public ConsoleColor InvalidRangeForegroundColor { get; set; } = ConsoleColor.Black;
    public TimeSpan MandatoryPause { get; set; } = TimeSpan.FromMinutes(30);


    public YieldHelperForMandatoryBreak(ConfigHost configHost)
    {
        _configHost = configHost;
    }

    private static TimeSpan GetTotalPause(IEnumerable<TimeLogLine> timeLogLines)
    {
        return timeLogLines.Aggregate(
            TimeSpan.Zero,
            (l, r) => l + (r.Project == Constants.ProjectForBreak
                ? r.TimeStampEnd - r.TimeStampStart
                : TimeSpan.Zero));
    }

    public TimeLogLine[] GetLinesWithMandatoryBreak(TimeLogFile timeLogLinesFile)
    {
        var lines = timeLogLinesFile.GetLines().ToArray();
        var totalPause = GetTotalPause(lines);
        if (totalPause >= MandatoryPause)
            return lines;

        var remainingMandatoryPause = MandatoryPause - totalPause;
        var line = GetTimeLogLineForPause(timeLogLinesFile, lines, totalPause, remainingMandatoryPause);
        return lines.InsertWithTimeShift(
                line,
                new TimeLogLine(
                    line.TimeStampEnd,
                    line.TimeStampEnd + remainingMandatoryPause,
                    Constants.ProjectForBreak,
                    Constants.MessageForBreak))
            .ToArray();
    }

    private TimeLogLine GetTimeLogLineForPause(
        TimeLogFile timeLogLinesFile,
        TimeLogLine[] lines,
        TimeSpan totalPause,
        TimeSpan remainingMandatoryPause)
    {
        if (_configHost.TryGet(
                typeof(YieldHelperForMandatoryBreak).FullName(),
                timeLogLinesFile.Date.ToString("yyyy-MM-dd"),
                out InsertPosition tuple) && tuple.TotalLines == lines.Length)
        {
            return lines[tuple.LineIndex];
        }

        Console.WriteLine($"Missing mandatory break time on {timeLogLinesFile.Date.ToLongDateString()}.");
        Console.WriteLine($"Got {totalPause} of break time, expected at least {MandatoryPause}.");
        Console.WriteLine($"Please choose where to append {remainingMandatoryPause} of mandatory break time.");
        Console.WriteLine($"Time will be appended AFTER the selected entry.");
        var maxBreakTime = lines.First().TimeStampStart.AddHours(6);
        var line = AskConsole.ForValueFromCollection(
            lines,
            (q) => new ConsoleString
            {
                Text = q.ToString(),
                Foreground = q.TimeStampEnd > maxBreakTime || q.TimeStampStart > maxBreakTime
                    ? InvalidRangeForegroundColor
                    : ValidRangeForegroundColor,
                Background = ListingBackgroundColor,
            });
        var lineIndex = Array.IndexOf(lines, line);
        _configHost.Set(
            typeof(YieldHelperForMandatoryBreak).FullName(),
            timeLogLinesFile.Date.ToString("yyyy-MM-dd"),
            new InsertPosition(lines.Length, lineIndex));
        return line;
    }
}