using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;

namespace QuickTrack.Exporters;

public abstract class ExporterBase
{
    /// <summary>
    /// The identifier of this exporter.
    /// May not be equivalent to "help".
    /// </summary>
    public abstract string Identifier { get; }

    /// <summary>
    /// The pattern (minus the export command and identifier),
    /// the arguments of this exporter expect.
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public string Pattern => $" FROM [ TO ] {ArgsPattern}";

    /// <summary>
    /// The pattern (minus the export command and identifier),
    /// the arguments of this exporter expect.
    /// </summary>
    protected abstract string ArgsPattern { get; }
    
    /// <summary>
    /// The text to return when help is requested.
    /// </summary>
    public abstract string HelpText { get; }

    /// <summary>
    /// Perform the actual export
    /// </summary>
    /// <param name="days">
    /// The days to be exported
    /// </param>
    /// <param name="args">
    /// The arguments passed into the exporter.
    /// Note: First argument may not be a date matching the pattern "dd.MM".
    /// </param>
    /// <param name="cancellationToken"></param>
    protected abstract ValueTask DoExportAsync(IEnumerable<Day> days, string[] args, CancellationToken cancellationToken);

    /// <summary>
    /// Parses the from and to arguments and executes the exporter.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="cancellationToken"></param>
    public async Task ExportAsync(string[] args, CancellationToken cancellationToken)
    {
        var argSkip = 0;
        if (args.Length is 0)
        {
            new ConsoleString(HelpText)
                {
                    Foreground = ConsoleColor.Cyan,
                    Background = ConsoleColor.Black,
                }
                .WriteLine();
            return;
        }
        if (!DateOnly.TryParseExact(
                args[0],
                new []{
                            Constants.DateFormatFormal,
                            Constants.DateFormatNoDot,
                            Constants.FullDateFormatFormal,
                            Constants.FullDateFormatNoDot
                        },
                CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var startDate))
        {
            new ConsoleString($"Failed to parse start-date {args[0]}.")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }
                .WriteLine();
            return;
        }

        argSkip++;
        if (args.Length < 2 || !DateOnly.TryParseExact(
                args[1],
                new []{
                            Constants.DateFormatFormal,
                            Constants.DateFormatNoDot,
                            Constants.FullDateFormatFormal,
                            Constants.FullDateFormatNoDot
                        },
                CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var endDate))
            endDate = startDate;
        else
            argSkip++;

        if (endDate < startDate)
        {
            new ConsoleString
            {
                Text       = $"End date ({endDate}) is after start date ({startDate})",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        var days = new List<Day>();
        await foreach (var day in QtRepository.GetDays(cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            if (day.Date < startDate)
                break;
            if (day.Date > endDate)
                continue;
            days.Add(day);
        }
        days.Sort((l, r) => l.Date.ToDateOnly().CompareTo(r.Date.ToDateOnly()));
        if (!days.Any())
        {
            new ConsoleString
            {
                Text       = $"No log file found for date range {startDate} - {endDate}",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        await DoExportAsync(days, args.Skip(argSkip).ToArray(), cancellationToken)
            .ConfigureAwait(false);
    }

    protected static string MakeOutputFolderPath(string outputFile)
    {
        Directory.CreateDirectory(Path.Combine(Programm.Workspace, OutputFolderName));
        return Path.Combine(Programm.Workspace, OutputFolderName, outputFile);
    }

    private const string OutputFolderName = "out";
}