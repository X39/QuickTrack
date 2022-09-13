using QuickTrack.Data.Database;
using X39.Util.Console;

namespace QuickTrack;

public static class Extensions
{
    public static async Task<ConsoleString> ToConsoleString(
        this TimeLog timeLog,
        Day day,
        ConsoleStringFormatter consoleStringFormatter,
        CancellationToken cancellationToken)
    {
        if (day.Id != timeLog.DayFk)
            throw new ArgumentException("Day does not match the one from the timeLog");
        var consoleString = await consoleStringFormatter.ToConsoleInputStringAsync(timeLog, cancellationToken);
        var time = timeLog.TimeStamp.ToString("[HH:mm]");
        var prefix = ConsoleStringFormatter.ToConsoleOutputPrefixString(day);
        return timeLog.Mode == ETimeLogMode.Break
            ? new ConsoleString
            {
                Text       = string.Concat(prefix, time, ' ', consoleString),
                Foreground = ConsoleColor.Gray,
                Background = ConsoleColor.Black,
            }
            : new ConsoleString
            {
                Text       = string.Concat(prefix, time, ' ', consoleString),
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