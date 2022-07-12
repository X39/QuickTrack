using System.Globalization;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class EditCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"edit", "modify", "nano"};

    public string Description =>
        "Opens a log file in the text-editor of your choice. " +
        $"Dates are to be provided in the format {Constants.DateFormatFormal} or {Constants.DateFormatNoDot}" +
        $" (eg. 21.02 or 2102)";

    public string Pattern => "( edit | modify | nano ) DATE { DATE }";

    public void Execute(string[] args)
    {
        var dates = new DateOnly[args.Length];
        var error = false;
        for (var i = 0; i < args.Length; i++)
        {
            var dateString = args[i];
            if (DateOnly.TryParseExact(
                    dateString,
                    new[] {Constants.DateFormatFormal, Constants.DateFormatNoDot},
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var date))
                dates[i] = date;
            else
            {
                new ConsoleString($"Failed to parse date string '{dateString}'")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                error = true;
            }
        }

        if (error)
            return;

        var logFiles = Programm.LoadLogFilesFromDisk();
        foreach (var date in dates)
        {
            var logFile = Programm.GetOfDateOrNull(logFiles, date);
            if (logFile is null)
                new ConsoleString($"Log file for {date} could not be found.")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            else
                logFile.OpenWithDefaultProgram();
        }
    }
}