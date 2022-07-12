#pragma warning disable CA2211
using JetBrains.Annotations;
using X39.Util.Console;

namespace QuickTrack;

[PublicAPI]
public static class YieldWithMandatoryBreakExtension
{
    public static ConsoleColor ListingBackgroundColor      = ConsoleColor.DarkCyan;
    public static ConsoleColor ValidRangeForegroundColor   = ConsoleColor.White;
    public static ConsoleColor InvalidRangeForegroundColor = ConsoleColor.Black;
    public static TimeSpan     MandatoryPause              = TimeSpan.FromMinutes(30);

    private static TimeSpan GetTotalPause(this IEnumerable<TimeLogLine> timeLogLines)
    {
        return timeLogLines.Aggregate(
            TimeSpan.Zero,
            (l, r) => l + (r.Project == Constants.ProjectForBreak ? r.TimeStampEnd - r.TimeStampStart : TimeSpan.Zero));
    }

    public static TimeLogLine[] GetLinesWithMandatoryBreak(this TimeLogFile timeLogLinesFile)
    {
        var lines = timeLogLinesFile.GetLines().ToArray();
        var totalPause = lines.GetTotalPause();
        if (totalPause >= MandatoryPause)
            return lines;
        var remainingMandatoryPause = MandatoryPause - totalPause;
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
        return lines.InsertWithTimeShift(
                line,
                new TimeLogLine(
                    line.TimeStampEnd,
                    line.TimeStampEnd + remainingMandatoryPause,
                    Constants.ProjectForBreak,
                    Constants.MessageForBreak))
            .ToArray();
    }
}