using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using QuickTrack.Win32;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack;

public static class Programm
{
    public static string Workspace => Environment.CurrentDirectory;
    public const string BreakTimeMessage = $"{BreakTimeMessageProject}:{BreakTimeMessageContent}";
    public const string BreakTimeMessageProject = "break";
    public const string BreakTimeMessageContent = "start.";

    public static void Main(string[] args)
    {
        new ConsoleString($"Working Directory: {Workspace}")
        {
            Foreground = ConsoleColor.DarkGray,
        }.WriteLine();

        var logFiles = LoadLogFilesFromDisk();

        logFiles.Sort((l, r) => l.Date.CompareTo(r.Date));
        var undoQueue = new Stack<string>();
        var redoQueue = new Stack<string>();
        var lastMessage = PrintTodayLogFile(logFiles, undoQueue);
        using var consoleCancellationTokenSource = new ConsoleCancellationTokenSource();
        if (lastMessage is not null && (IsBreakMessage(lastMessage)))
        {
            var now = DateTime.Now;
            var minutesPassed = (now - lastMessage.TimeStampStart).TotalMinutes;
            PrintBreakMessage(
                lastMessage.TimeStampStart,
                minutesPassed > 30
                    ? now
                    : now.AddMinutes(30 - minutesPassed));
        }

        var configHost = new ConfigHost(Workspace);
        var quickTrackHost = new QuickTrackHost(Workspace, lastMessage);
        var commandParser = new CommandParser(new UnmatchedCommand(
            "project:message | message",
            "Appends a new line to the log. If project is omitted, the previous one will be used.",
            quickTrackHost.TryAppendNewLogLine));
        commandParser.RegisterCommand(
            new[] {"pause", "break"},
            "pause | break",
            "Starts a break and switches time-logging to the pause mode.",
            quickTrackHost.StartBreak);
        commandParser.RegisterCommand(
            new[] {"rewrite", "reword"},
            "rewrite | reword",
            "Allows to change the description from any time log line of today.",
            RewordLogLineOfToday);
        commandParser.RegisterCommand(
            new[] {"quit", "end", "exit"},
            "quit | end | exit",
            "Writes 'end of day' message and terminates the program.",
            (_) =>
            {
                quickTrackHost.TryAppendNewLogLine("quit");
                // ReSharper disable once AccessToDisposedClosure
                consoleCancellationTokenSource.Cancel();
            });
        commandParser.RegisterCommand(
            new[] {"list"},
            "list [ week | month ]",
            "Lists the logged entries. " +
            "If no argument is provided, only today and yesterday will be listed. " +
            "If week is provided, the previous 7 days will be listed. " +
            "If month is provided, the previous 31 days will be listed.",
            ListCommand);
        commandParser.RegisterCommand(
            new[] {"undo"},
            "undo",
            "Lists the logged entries. " +
            "Removes the last log line of today. " +
            "If no more log lines are available for a day to be removed, nothing will be done.",
            UndoCommand);
        commandParser.RegisterCommand(
            new[] {"sap"},
            () => $"sap FROM [ TO ] [ {(SapExporter.WriteLineByDefault(configHost) ? "no" : "NO")} | " +
                  $"{(SapExporter.WriteLineByDefault(configHost) ? "YES" : "yes")} ]",
            "Exports the times to sap. " +
            "FROM and TO are expected to be in the format DD.MM (eg. 13.06). " +
            "FROM and TO are both inclusive. " +
            "If the word 'no' is appended, no log line about the export will be written for today. " +
            "If the word 'yes' is appended, a log line about the export will be written, denoting " +
            "both the start and end of the actual export (and restoring the previous line). " +
            $"The default value here is {(SapExporter.WriteLineByDefault(configHost) ? "yes" : "no")} " +
            "and is bound to the previous export action. " +
            "If TO is not provided, only the FROM day will be exported.",
            strings => SapCommand(configHost, quickTrackHost, strings));
        commandParser.RegisterCommand(
            new[] {"edit", "modify"},
            () => "( edit | modify ) DATE { DATE }",
            "Opens a log file in the text-editor of your choice. " +
            "Dates are to be provided in the format DD.MM (eg. 21.02)",
            EditCommand);

        while (!consoleCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                commandParser.PromptCommand(undoQueue, redoQueue, consoleCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                new ConsoleString(ex.Message)
                        {Foreground = ConsoleColor.Red, Background = ConsoleColor.White}
                    .WriteLine();
                if (ex.StackTrace is not null)
                {
                    new ConsoleString(ex.StackTrace)
                            {Foreground = ConsoleColor.Red, Background = ConsoleColor.White}
                        .WriteLine();
                }
            }
        }
    }

    private static void EditCommand(string[] obj)
    {
        var dates = new DateOnly[obj.Length];
        var error = false;
        for (var i = 0; i < obj.Length; i++)
        {
            var dateString = obj[i];
            if (DateOnly.TryParseExact(
                    dateString,
                    "dd.MM",
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

        var logFiles = LoadLogFilesFromDisk();
        foreach (var date in dates)
        {
            var logFile = GetOfDateOrNull(logFiles, date);
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

    private static void UndoCommand()
    {
        if (RemoveLastLineOfToday())
        {
            new ConsoleString(
                $"Undid last line.")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Green,
            }.WriteLine();
        }
        else
        {
            new ConsoleString(
                $"No line found to undo.")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Red,
            }.WriteLine();
        }
    }

    private static void ListCommand(string[] commandArgs)
    {
        var first = commandArgs.FirstOrDefault();
        var days = first?.ToLower() switch
        {
            "week" => 7,
            "month" => 31,
            _ => 0,
        };
        var logFiles = LoadLogFilesFromDisk()
            .Where((q) => (DateTime.Today - q.Date.ToDateTime(TimeOnly.MinValue)).Days <= days)
            .OrderBy((q) => q.Date)
            .ToList();
        foreach (var logFile in logFiles)
        {
            var tag = GetPrettyPrintTag(logFile);
            PrintLogFile(logFile, tag);
        }
    }

    private static void SapCommand(ConfigHost configHost, QuickTrackHost quickTrackHost, string[] args)
    {
        DateOnly startDate;
        DateOnly endDate;
        var writeLog = true;
        switch (args.Length)
        {
            case 1:
                try
                {
                    startDate = endDate = DateOnly.ParseExact(
                        args[0],
                        "dd.MM",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces);
                }
                catch (Exception ex)
                {
                    new ConsoleString($"Parsing failed: {ex.Message}.")
                            {Foreground = ConsoleColor.Red, Background = ConsoleColor.Black}
                        .WriteLine();
                }

                break;
            case 2 when args[1].ToLower() is not "yes" and not "no":
                try
                {
                    startDate = DateOnly.ParseExact(
                        args[0],
                        "dd.MM",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces);
                    endDate = DateOnly.ParseExact(
                        args[1],
                        "dd.MM",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces);
                }
                catch (Exception ex)
                {
                    new ConsoleString($"Parsing failed: {ex.Message}.")
                            {Foreground = ConsoleColor.Red, Background = ConsoleColor.Black}
                        .WriteLine();
                }

                break;
            case 2:
                try
                {
                    startDate = endDate = DateOnly.ParseExact(
                        args[0],
                        "dd.MM",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces);
                    writeLog = args[1].ToLower() switch
                    {
                        "y" => true,
                        "yes" => true,
                        "n" => false,
                        "no" => false,
                        _ => throw new FormatException("Expected either 'yes' or 'no'"),
                    };
                }
                catch (Exception ex)
                {
                    new ConsoleString($"Parsing failed: {ex.Message}.")
                            {Foreground = ConsoleColor.Red, Background = ConsoleColor.Black}
                        .WriteLine();
                }

                break;
            case 3:
                try
                {
                    startDate = DateOnly.ParseExact(
                        args[0],
                        "dd.MM",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces);
                    endDate = DateOnly.ParseExact(
                        args[1],
                        "dd.MM",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.AllowWhiteSpaces);
                    writeLog = args[2].ToLower() switch
                    {
                        "y" => true,
                        "yes" => true,
                        "n" => false,
                        "no" => false,
                        _ => throw new FormatException("Expected either 'yes' or 'no'"),
                    };
                }
                catch (Exception ex)
                {
                    new ConsoleString($"Parsing failed: {ex.Message}.")
                            {Foreground = ConsoleColor.Red, Background = ConsoleColor.Black}
                        .WriteLine();
                }

                break;
            default:
                new ConsoleString("Insufficient args.")
                        {Foreground = ConsoleColor.Red, Background = ConsoleColor.Black}
                    .WriteLine();
                return;
        }

        if (SapExporter.WriteLineByDefault(configHost) != writeLog)
            SapExporter.WriteLineByDefault(configHost, writeLog);

        var files = Directory.GetFiles(Workspace, "*.tlog", SearchOption.TopDirectoryOnly);
        var logFiles = LoadLogFilesFromDisk(files);
        var selectedLogFile = logFiles
            .Where((q) => q.Date >= startDate)
            .Where((q) => q.Date <= endDate)
            .ToArray();
        if (!selectedLogFile.Any())
        {
            new ConsoleString
            {
                Text = $"No log file found for date range {startDate} - {endDate}",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.White,
            }.WriteLine();
            return;
        }

        if (writeLog)
        {
            var previousLine = GetLastLineOfToday(logFiles);
            var dates = selectedLogFile
                .Select((q) => q.Date)
                .Select((q) => q.ToString("dd.MM"));
            // ReSharper disable once StringLiteralTypo
            quickTrackHost.TryAppendNewLogLine($"SAP-BBD: Zeiterfassung {string.Join(", ", dates)}");
            new SapExporter(configHost, selectedLogFile)
                .StartExport();
            if (previousLine is not null)
            {
                quickTrackHost.TryAppendNewLogLine($"{previousLine.Project}:{previousLine.Message}");
            }
        }
        else
        {
            new SapExporter(configHost, selectedLogFile)
                .StartExport();
        }
    }

    public static TimeLogFile? GetLogFileOfToday()
        => GetLogFileOfToday(GetLogFiles(), out _);

    public static TimeLogFile? GetLogFileOfToday(out List<TimeLogFile> logFiles)
        => GetLogFileOfToday(GetLogFiles(), out logFiles);

    public static TimeLogFile? GetLogFileOfToday(string[] files, out List<TimeLogFile> logFiles)
    {
        logFiles = LoadLogFilesFromDisk(files);
        var todayLogFile = logFiles.FirstOrDefault((q) => q.Date == DateTime.Today.ToDateOnly());
        if (todayLogFile is null)
        {
            new ConsoleString
            {
                Text = "No log file for today was located.",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return todayLogFile;
        }

        return todayLogFile;
    }

    private static void RewordLogLineOfToday()
    {
        var todayLogFile = GetLogFileOfToday();
        if (todayLogFile is null)
            return;
        RewordLogLineOf(todayLogFile);
    }

    private static void RewordLogLineOf(TimeLogFile todayLogFile)
    {
        var timeLogLines = todayLogFile.GetLines().ToArray();
        var timeLogLine = AskConsole.ForValueFromCollection(
            timeLogLines,
            tToString: (timeLogLine) => new ConsoleString
            {
                Text = timeLogLine.ToString(),
                Foreground = ConsoleColor.DarkBlue,
                Background = ConsoleColor.Gray,
            },
            invalidSelectionText: new ConsoleString
            {
                Text = "Invalid element. Please select one to continue",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Gray,
            });

        new ConsoleString
        {
            Text = "Selected ",
            Foreground = ConsoleColor.Black,
            Background = ConsoleColor.Gray,
        }.Write();
        new ConsoleString
        {
            Text = timeLogLine.ToString(),
            Foreground = ConsoleColor.DarkBlue,
            Background = ConsoleColor.Gray,
        }.WriteLine();
        new ConsoleString
        {
            Text = "Please enter the new non-empty Description:",
            Foreground = ConsoleColor.Black,
            Background = ConsoleColor.Gray,
        }.WriteLine();
        new ConsoleString
        {
            Text = "> ",
            Foreground = ConsoleColor.Black,
            Background = ConsoleColor.Gray,
        }.Write();
        var newDescription = Console.ReadLine()?.Trim();
        if (newDescription.IsNullOrWhiteSpace())
        {
            new ConsoleString
            {
                Text = "Cannot change to empty description.",
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        todayLogFile.Clear();
        foreach (var logLine in timeLogLines)
        {
            if (logLine == timeLogLine)
                todayLogFile.Append(timeLogLine with
                {
                    Message = newDescription,
                });
            else
                todayLogFile.Append(logLine);
        }

        return;
    }

    private static string GetPrettyPrintTag(TimeLogFile logFile)
    {
        var tag = (DateTime.Today - logFile.Date.ToDateTime(TimeOnly.MinValue)).Days switch
        {
            0 => "[TOD]",
            1 => "[YST]",
            _ => logFile.Date.DayOfWeek switch
            {
                DayOfWeek.Monday => "[MON]",
                DayOfWeek.Tuesday => "[TUE]",
                DayOfWeek.Wednesday => "[WED]",
                DayOfWeek.Thursday => "[THU]",
                DayOfWeek.Friday => "[FRI]",
                DayOfWeek.Saturday => "[SAT]",
                DayOfWeek.Sunday => "[SUN]",
                _ => throw new ArgumentOutOfRangeException(),
            },
        };
        return $"[{logFile.Date:dd.MM}]{tag}";
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

    public static string[] GetLogFiles()
    {
        var files = Directory.GetFiles(Workspace, "*", SearchOption.TopDirectoryOnly);
        return files
            .Where((file) => TimeLogFile.Extensions.Contains(Path.GetExtension(file)))
            .ToArray();
    }

    public static List<TimeLogFile> LoadLogFilesFromDisk()
    {
        var files = GetLogFiles();
        return LoadLogFilesFromDisk(files);
    }

    public static List<TimeLogFile> LoadLogFilesFromDisk(string[] files)
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

    private static TimeLogFile? GetOfDateOrNull(IEnumerable<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        return logFiles.FirstOrDefault((q) => q.Date == dateOnly);
    }

    private static TimeLogFile? GetOfDatePriorToOrNull(IEnumerable<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        return logFiles.LastOrDefault((q) => q.Date < dateOnly);
    }

    private static TimeLogFile GetOfDate(ICollection<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        var filePath = Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd"));
        return GetOfDateOrNull(logFiles, dateOnly)
               ?? logFiles.AddAndReturn(new TimeLogFile(filePath, dateOnly));
    }

    private static TimeLogFile GetOfDatePriorTo(ICollection<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        var filePath = Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd"));
        return GetOfDatePriorToOrNull(logFiles, dateOnly)
               ?? logFiles.AddAndReturn(new TimeLogFile(filePath, dateOnly));
    }

    private static TimeLogLine? GetLastLineOfToday(ICollection<TimeLogFile> logFiles)
    {
        var today = GetOfDate(logFiles, DateTime.Today.ToDateOnly());
        return today.GetLines().LastOrDefault();
    }

    private static bool RemoveLastLineOfToday()
        => RemoveLastLineOfToday(GetLogFileOfToday().MakeEnumerable().NotNull().ToList());

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

    private static TimeLogLine? PrintLogFile(TimeLogFile logFile, string? prefix = null)
    {
        var now = DateTime.Now.ToUniversalTime();
        TimeLogLine? lastLine = null;

        foreach (var timeLogLine in logFile?.GetLines() ?? Enumerable.Empty<TimeLogLine>())
        {
            lastLine = timeLogLine;
            if (prefix is not null)
            {
                Print(timeLogLine, prefix,
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

    private static TimeLogLine? PrintTodayLogFile(
        ICollection<TimeLogFile> logFiles,
        Stack<string> undoQueue)
    {
        var now = DateTime.Now.ToUniversalTime();
        var today = now.ToDateOnly();
        var previousDayLogFile = GetOfDatePriorToOrNull(logFiles, today);
        var lastLogFile = GetOfDate(logFiles, today);
        TimeLogLine? lastLine = null;

        undoQueue.Clear();
        foreach (var timeLogLine in previousDayLogFile?.GetLines() ?? Enumerable.Empty<TimeLogLine>())
        {
            lastLine = timeLogLine;
            Print(timeLogLine, GetPrettyPrintTag(previousDayLogFile!),
                foreground: ConsoleColor.DarkYellow,
                foregroundBreak: ConsoleColor.DarkGray);
            undoQueue.Push($"{timeLogLine.Project}: {timeLogLine.Message}");
        }

        foreach (var timeLogLine in lastLogFile.GetLines())
        {
            lastLine = timeLogLine;
            Print(timeLogLine);
            undoQueue.Push($"{timeLogLine.Project}: {timeLogLine.Message}");
        }

        return lastLine;
    }

    public static void Print(
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

    private static bool IsBreakMessage(TimeLogLine timeLogLine)
    {
        return timeLogLine.Project == BreakTimeMessageProject &&
               timeLogLine.Message == BreakTimeMessageContent;
    }
}