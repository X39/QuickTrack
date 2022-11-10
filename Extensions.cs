using QuickTrack.Data.Database;
using X39.Util.Console;

namespace QuickTrack;

public static class Extensions
{
    public static async Task<ConsoleString> ToConsoleString(
        this TimeLog timeLogCurrent,
        Day day,
        ConsoleStringFormatter consoleStringFormatter,
        CancellationToken cancellationToken,
        TimeLog? timeLogNext = null)
    {
        if (day.Id != timeLogCurrent.DayFk)
            throw new ArgumentException("Day does not match the one from the timeLogCurrent");
        var consoleString = await consoleStringFormatter.ToConsoleInputStringWithLocationAsync(timeLogCurrent, cancellationToken);
        var timeCurrent = timeLogCurrent.TimeStamp.ToString("HH:mm");
        var timeNext = timeLogNext?.TimeStamp.ToString("HH:mm") ?? "--:--";
        var prefix = ConsoleStringFormatter.ToConsoleOutputPrefixString(day);
        return timeLogCurrent.Mode == ETimeLogMode.Break
            ? new ConsoleString
            {
                Text       = string.Concat(prefix, '[', timeCurrent, " - ", timeNext, ']', consoleString),
                Foreground = ConsoleColor.Gray,
                Background = ConsoleColor.Black,
            }
            : new ConsoleString
            {
                Text       = string.Concat(prefix, '[', timeCurrent, " - ", timeNext, ']', consoleString),
                Foreground = ConsoleColor.DarkYellow,
                Background = ConsoleColor.Black,
            };
    }
    
    public static DateTime RoundToMinutes(this DateTime self) => new(
        self.Year,
        self.Month,
        self.Day,
        self.Hour,
        self.Minute,
        0);
}