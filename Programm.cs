using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuickTrack.Commands;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using QuickTrack.Exporters;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack;

// .\notepad++.exe -multiInst -notabbar -nosession .\shortcuts.xml
public static class Programm
{
    public static string Workspace => Environment.CurrentDirectory;
    public static QuickTrackHost Host { get; private set; } = null!;

    public const string BreakTimeMessage        = $"{BreakTimeMessageProject}:{BreakTimeMessageContent}";
    public const string BreakTimeMessageProject = Constants.ProjectForBreak;
    public const string BreakTimeMessageContent = "start.";

    private static int                             ReturnCode = Constants.ErrorCodes.Ok;
    private static ConsoleCancellationTokenSource? CancellationTokenSource;

    public static void Quit(int returnCode = 0)
    {
        ReturnCode = returnCode;
        CancellationTokenSource?.Cancel();
    }


    public static async Task<int> Main(string[] args)
    {
        await using var tokenSource = new ConsoleCancellationTokenSource();
        CancellationTokenSource = tokenSource;
        if (!await EnsureDatabaseCreatedOrReturnFalseToExit(CancellationTokenSource.Token))
            return Constants.ErrorCodes.DatabaseFailedToCreate;

        try
        {
            var startLocation = await Prompt.ForLocationIfMultipleAsync(CancellationTokenSource.Token);
            var lastLineTuple = await PrintRecentAndGetLastLineWritten(CancellationTokenSource.Token);

            var quickTrackHost = Host = new QuickTrackHost(Workspace, lastLineTuple, startLocation);
            quickTrackHost.CommandParser.RegisterCommand<UndoCommand>();
            quickTrackHost.CommandParser.RegisterCommand<ListCommand>();
            quickTrackHost.CommandParser.RegisterCommand<SearchCommand>();
            quickTrackHost.CommandParser.RegisterCommand<SearchProjectCommand>();
            quickTrackHost.CommandParser.RegisterCommand<EditCommand>();
            quickTrackHost.CommandParser.RegisterCommand<TotalCommand>();
            quickTrackHost.CommandParser.RegisterCommand<ExportCommand>();
            quickTrackHost.CommandParser.RegisterCommand<ProjectCommand>();
            quickTrackHost.CommandParser.RegisterCommand<LocationCommand>();
            quickTrackHost.CommandParser.RegisterCommand<RdpActiveCommand>();

            await quickTrackHost.RunAsync(CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // empty
        }

        return ReturnCode;
    }


    public static void PrintBreakMessage(DateTime from, DateTime to)
    {
        new ConsoleString(
            $"{from.ToLocalTime().ToTimeOnly()} to {to.ToLocalTime().ToTimeOnly()} -- BREAK MODE -- HIT ENTER TO CONTINUE")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.DarkGreen,
        }.WriteLine();
    }

    #region Obsolete

    [Obsolete]
    private static TimeLogFile? GetLogFileOfToday()
        => GetLogFileOfToday(GetLogFiles(), out _);

    [Obsolete]
    private static TimeLogFile? GetLogFileOfToday(out List<TimeLogFile> logFiles)
        => GetLogFileOfToday(GetLogFiles(), out logFiles);

    [Obsolete]
    private static TimeLogFile? GetLogFileOfToday(string[] files, out List<TimeLogFile> logFiles)
    {
        logFiles = LoadLogFilesFromDisk(files);
        var todayLogFile = logFiles.FirstOrDefault((q) => q.Date == DateTime.Today.ToDateOnly());
        if (todayLogFile is not null)
            return todayLogFile;
        new ConsoleString
        {
            Text       = "No log file for today was located.",
            Foreground = ConsoleColor.Red,
            Background = ConsoleColor.Black,
        }.WriteLine();
        return todayLogFile;
    }

    [Obsolete("Obsolete")]
    private static List<TimeLogFile> LoadLogFilesFromDisk()
    {
        var files = GetLogFiles();
        return LoadLogFilesFromDisk(files);
    }

    [Obsolete("Obsolete")]
    private static List<TimeLogFile> LoadLogFilesFromDisk(string[] files)
    {
        var logFiles = new List<TimeLogFile>();
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (DateTime.TryParseExact(
                    fileName,
                    "yyyy-MM-dd",
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.AssumeUniversal,
                    out var dateTime))
            {
                logFiles.Add(new TimeLogFile(file, dateTime.ToDateOnly()));
            }
            else
            {
                new ConsoleString($"Failed to parse date of: {Path.GetFileName(file)}")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red,
                }.WriteLine();
            }
        }

        return logFiles;
    }

    [Obsolete]
    private static TimeLogFile? GetOfDateOrNull(IEnumerable<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        return logFiles.FirstOrDefault((q) => q.Date == dateOnly);
    }

    [Obsolete]
    private static TimeLogFile? GetOfDatePriorToOrNull(IEnumerable<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        return logFiles.LastOrDefault((q) => q.Date < dateOnly);
    }

    [Obsolete]
    private static TimeLogFile GetOfDate(ICollection<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        var filePath = Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd"));
        if (logFiles.IsReadOnly)
        {
            return GetOfDateOrNull(logFiles, dateOnly)
                   ?? new TimeLogFile(filePath, dateOnly);
        }
        else
        {
            return GetOfDateOrNull(logFiles, dateOnly)
                   ?? logFiles.AddAndReturn(new TimeLogFile(filePath, dateOnly));
        }
    }

    [Obsolete]
    private static TimeLogFile GetOfDatePriorTo(ICollection<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        var filePath = Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd"));
        if (logFiles.IsReadOnly)
        {
            return GetOfDatePriorToOrNull(logFiles, dateOnly)
                   ?? new TimeLogFile(filePath, dateOnly);
        }
        else
        {
            return GetOfDatePriorToOrNull(logFiles, dateOnly)
                   ?? logFiles.AddAndReturn(new TimeLogFile(filePath, dateOnly));
        }
    }

    [Obsolete]
    private static TimeLogLine? GetLastLineOfToday(ICollection<TimeLogFile> logFiles)
    {
        var today = GetOfDate(logFiles, DateTime.Today.ToDateOnly());
        return today.GetLines().LastOrDefault();
    }

    [Obsolete]
    private static bool RemoveLastLineOfToday()
        => RemoveLastLineOfToday(GetLogFileOfToday().MakeEnumerable().NotNull().ToList());

    [Obsolete]
    private static bool RemoveLastLineOfToday(ICollection<TimeLogFile> logFiles)
    {
        var today = GetOfDate(logFiles, DateTime.Today.ToDateOnly());
        var lines = File.Exists(today.FilePath)
            ? File.ReadAllLines(today.FilePath)
            : Array.Empty<string>();
        using var stream = File.Open(today.FilePath, FileMode.Create);
        using var writer = new StreamWriter(stream);
        var success = false;
        foreach (var line in lines.Take(lines.Length - 1))
        {
            writer.WriteLine(line);
            success = true;
        }

        return success;
    }

    [Obsolete]
    private static TimeLogLine? PrintLogFile(TimeLogFile logFile, string? prefix = null)
    {
        var now = DateTime.Now.ToUniversalTime();
        TimeLogLine? lastLine = null;

        foreach (var timeLogLine in logFile?.GetLines() ?? Enumerable.Empty<TimeLogLine>())
        {
            lastLine = timeLogLine;
            if (prefix is not null)
            {
                Print(
                    timeLogLine,
                    prefix,
                    foreground: ConsoleColor.DarkYellow,
                    foregroundBreak: ConsoleColor.DarkGray);
            }
            else
            {
                Print(timeLogLine);
            }
        }

        return lastLine;
    }

    [Obsolete]
    private static void Print(
        TimeLogLine timeLogLine,
        string? prefix = null,
        ConsoleColor? foreground = null,
        ConsoleColor? foregroundBreak = null,
        ConsoleColor? background = null,
        ConsoleColor? backgroundBreak = null)
    {
        new ConsoleString(string.Concat(prefix, timeLogLine))
        {
            Foreground = IsBreakMessage(timeLogLine)
                ? foregroundBreak ?? ConsoleColor.Gray
                : foreground ?? ConsoleColor.Yellow,
            Background = IsBreakMessage(timeLogLine)
                ? backgroundBreak ?? ConsoleColor.Black
                : background ?? ConsoleColor.Black,
        }.WriteLine();
    }

    [Obsolete]
    private static bool IsBreakMessage(TimeLogLine timeLogLine)
    {
        return timeLogLine.Project == BreakTimeMessageProject &&
               timeLogLine.Message == BreakTimeMessageContent;
    }

    [Obsolete]
    private static string[] GetLogFiles()
    {
        var files = Directory.GetFiles(Workspace, "*", SearchOption.TopDirectoryOnly);
        return files
            .Where((file) => TimeLogFile.Extensions.Contains(Path.GetExtension(file)))
            .ToArray();
    }

    #endregion

    /// <summary>
    ///     Prints the log file of today plus the previous <see cref="Day"/> logged
    ///     and returns the last <see cref="TimeLog"/> logged.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken"/> to cancel the operation at any point in time.
    /// </param>
    /// <returns>
    ///     The last <see cref="TimeLog"/> that was logged
    ///     or <see langword="null"/> if no such <see cref="TimeLog"/> exists.
    /// </returns>
    private static async Task<(Project Project, TimeLog TimeLog)?> PrintRecentAndGetLastLineWritten(
        CancellationToken cancellationToken)
    {
        var today = await QtRepository.GetTodayAsync(null, cancellationToken);
        var yesterday = await today.GetRelativeDayAsync(-1, cancellationToken);

        await using var consoleStringFormatter = new ConsoleStringFormatter();

        TimeLog? lastLine = null;
        TimeLog? previousProjectTimeLog = null;
        if (yesterday is not null)
        {
            TimeLog? previous = null;
            await foreach (var timeLog in yesterday.GetTimeLogs(cancellationToken))
            {
                if (previous is not null)
                {
                    var consoleString = await previous.ToConsoleString(
                        yesterday,
                        consoleStringFormatter,
                        cancellationToken,
                        timeLog);
                    consoleString.WriteLine();
                }

                previous = lastLine = timeLog;
                previousProjectTimeLog = timeLog.Mode == ETimeLogMode.Normal
                    ? timeLog
                    : previousProjectTimeLog;
            }

            if (previous is not null)
            {
                var consoleString = await previous.ToConsoleString(
                    yesterday,
                    consoleStringFormatter,
                    cancellationToken);
                consoleString.WriteLine();
            }
        }

        {
            TimeLog? previous = null;
            await foreach (var timeLog in today.GetTimeLogs(cancellationToken))
            {
                if (previous is not null)
                {
                    var consoleString = await previous.ToConsoleString(
                        today,
                        consoleStringFormatter,
                        cancellationToken,
                        timeLog);
                    consoleString.WriteLine();
                }

                previous = lastLine = timeLog;
                previousProjectTimeLog = timeLog.Mode == ETimeLogMode.Normal
                    ? timeLog
                    : previousProjectTimeLog;
            }

            if (previous is not null)
            {
                var consoleString = await previous.ToConsoleString(
                    today,
                    consoleStringFormatter,
                    cancellationToken);
                consoleString.WriteLine();
            }
        }

        if (lastLine is null)
            return null;

        return (
            previousProjectTimeLog is not null
                ? await consoleStringFormatter.GetProjectAsync(previousProjectTimeLog.ProjectFk, cancellationToken)
                : null,
            lastLine);
    }

    private static async Task<bool> EnsureDatabaseCreatedOrReturnFalseToExit(CancellationToken cancellationToken)
    {
        new ConsoleString($"Working Directory: {Workspace}")
        {
            Foreground = ConsoleColor.DarkGray,
        }.WriteLine();
        var databaseFile = Path.GetFullPath(Constants.DatabaseFile);
        if (File.Exists(databaseFile))
        {
            await using var dbContext = new QtContext();
            await dbContext.Database.MigrateAsync(cancellationToken);
            return true;
        }
        else
        {
            ConsoleKeyInfo answer;
            do
            {
                new ConsoleString($"No database found at {databaseFile}. Do you want to create it now? (y/n)")
                {
                    Foreground = ConsoleColor.Cyan,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                answer = Console.ReadKey(true);
            } while (answer.Key != ConsoleKey.Y && answer.Key != ConsoleKey.N);

            if (answer.Key is ConsoleKey.N)
                return false;

            await using var dbContext = new QtContext();
            await dbContext.Database.MigrateAsync(cancellationToken);
            do
            {
                new ConsoleString($"Do you want to perform a migration from the old flat-file format? (y/n)")
                {
                    Foreground = ConsoleColor.Cyan,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                answer = Console.ReadKey(true);
            } while (answer.Key != ConsoleKey.Y && answer.Key != ConsoleKey.N);

            if (answer.Key is ConsoleKey.N)
                return true;
            await new Migrator().MigrateFromFlatFileAsync();

            return true;
        }
    }

    private class Migrator
    {
        /// <summary>
        /// Temporary method to migrate to the new db-driven format.
        /// </summary>
        public async Task MigrateFromFlatFileAsync()
        {
#pragma warning disable CS0618
#pragma warning disable CS0612
            var configHost = new ConfigHost(Path.Combine(Workspace, "config.cfg"));
            var existingLogFiles = LoadLogFilesFromDisk();
#pragma warning restore CS0612
#pragma warning restore CS0618
            await using var dbContext = new QtContext();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
            var location = new Location
            {
                Title            = "Unknown",
                TimeStampCreated = DateTime.Now,
            };
            dbContext.Locations.Add(location);
            await dbContext.SaveChangesAsync();
            foreach (var existingLogFile in existingLogFiles)
            {
                var day = await existingLogFile.Date.GetDayAsync(this)
                    .ConfigureAwait(false);
                await day.AppendAuditAsync(this, EAuditKind.Note, "Migrating from flat-file.", CancellationToken.None)
                    .ConfigureAwait(false);

                var dayJsonAttachment = await day.GetJsonAttachmentAsync(typeof(SapBbdExport).FullName())
                    .ConfigureAwait(false);
                await dayJsonAttachment.WithDoAsync(
                    (YieldHelperForMandatoryBreak.JsonPayload payload, CancellationToken _) =>
                    {
                        if (!configHost.TryGet(
                                "QuickTrack.YieldHelperForMandatoryBreak",
                                $"Mapping@{day.Date.Year:0000}-{day.Date.Month:00}-{day.Date.Day:00}",
                                out var json)) return ValueTask.CompletedTask;
                        var bytes = Encoding.UTF8.GetBytes(json);
                        var utf8Reader = new Utf8JsonReader(bytes.AsSpan());
                        var jsonElement = JsonElement.ParseValue(ref utf8Reader);
                        payload.Insertions.Add(
                            0,
                            new YieldHelperForMandatoryBreak.JsonPayload.IntervalData
                            {
                                IntervalCount     = jsonElement.GetProperty("TotalLines").GetInt32(),
                                AfterTimeLogIndex = jsonElement.GetProperty("LineIndex").GetInt32(),
                            });
                        return ValueTask.CompletedTask;
                    });
                await dayJsonAttachment.UpdateAsync(default)
                    .ConfigureAwait(false);

                foreach (var logLine in existingLogFile.GetLines())
                {
                    var project = await logLine.Project.Trim().GetProjectAsync();
                    var projectJsonAttachment = await project.GetJsonAttachmentAsync(typeof(SapBbdExport).FullName())
                        .ConfigureAwait(false);
                    await projectJsonAttachment.WithDoAsync(
                        (SapBbdExport.JsonPayload payload, CancellationToken _) =>
                        {
                            if (!configHost.TryGet(
                                    "QuickTrack.SapExporter",
                                    $"Mapping@{logLine.Project}",
                                    out var json)) return ValueTask.CompletedTask;
                            var bytes = Encoding.UTF8.GetBytes(json);
                            var utf8Reader = new Utf8JsonReader(bytes.AsSpan());
                            var jsonElement = JsonElement.ParseValue(ref utf8Reader);
                            payload.ProjectCode    = jsonElement.GetProperty("Project").GetString();
                            payload.ProfessionCode = jsonElement.GetProperty("Profession").GetString();

                            return ValueTask.CompletedTask;
                        });
                    await projectJsonAttachment.UpdateAsync(default)
                        .ConfigureAwait(false);
                    await day.AppendTimeLogAsync(
                            this,
                            location,
                            project,
                            logLine.IsPause
                                ? ETimeLogMode.Break
                                : logLine.Message == "quit."
                                    ? ETimeLogMode.Quit
                                    : logLine.Project == "SAP-BBD"
                                        ? ETimeLogMode.Export
                                        : ETimeLogMode.Normal,
                            logLine.Message,
                            logLine.TimeStampStart)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}