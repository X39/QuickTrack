using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    /// <param name="timeLogFiles">
    /// The log-files to be exported
    /// </param>
    /// <param name="args">
    /// The arguments passed into the exporter.
    /// Note: First argument may not be a date matching the pattern "dd.MM".
    /// </param>
    protected abstract void DoExport(IEnumerable<TimeLogFile> timeLogFiles, string[] args);

    /// <summary>
    /// Parses the from and to arguments and executes the exporter.
    /// </summary>
    /// <param name="args"></param>
    public void Export(string[] args)
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
                new []{Constants.DateFormatFormal, Constants.DateFormatNoDot},
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
                new []{Constants.DateFormatFormal, Constants.DateFormatNoDot},
                CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var endDate))
            endDate = startDate;
        else
            argSkip++;

        var logFiles = Programm.LoadLogFilesFromDisk();
        var selectedLogFiles = logFiles
            .Where((q) => q.Date >= startDate)
            .Where((q) => q.Date <= endDate)
            .ToArray();
        if (!selectedLogFiles.Any())
        {
            new ConsoleString
            {
                Text       = $"No log file found for date range {startDate} - {endDate}",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        DoExport(selectedLogFiles, args.Skip(argSkip).ToArray());
    }

    protected static string MakeOutputFolderPath(string outputFile)
    {
        Directory.CreateDirectory(Path.Combine(Programm.Workspace, OutputFolderName));
        return Path.Combine(Programm.Workspace, OutputFolderName, outputFile);
    }

    private const string OutputFolderName = "out";
    
    protected ConfigHost ConfigHost { get; }

    protected ExporterBase(ConfigHost configHost)
    {
        ConfigHost = configHost;
    }
}