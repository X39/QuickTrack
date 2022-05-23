using System.Collections.Immutable;
using System.Globalization;
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

        var files = Directory.GetFiles(Workspace, "*.tlog", SearchOption.TopDirectoryOnly);
        var logFiles = LoadLogFilesFromDisk(files);

        logFiles.Sort((l, r) => l.Date.CompareTo(r.Date));
        var undoQueue = new Stack<string>();
        var redoQueue = new Stack<string>();
        var lastMessage = PrintTodayLogFile(logFiles, undoQueue);
        using var consoleCancellationTokenSource = new ConsoleCancellationTokenSource();
        var isBreak = false;
        if (lastMessage is not null && (isBreak = IsBreakMessage(lastMessage)))
        {
            var now = DateTime.Now;
            var minutesPassed = (now - lastMessage.TimeStampStart).TotalMinutes;
            PrintBreakMessage(
                lastMessage.TimeStampStart,
                minutesPassed > 30
                    ? now
                    : now.AddMinutes(30 - minutesPassed));
        }

        while (!consoleCancellationTokenSource.IsCancellationRequested)
        {
            var line = InteractiveConsoleInput.ReadLine(undoQueue, redoQueue, consoleCancellationTokenSource.Token);
            undoQueue.Push(line);
            if (isBreak)
            {
                line = string.Concat(lastMessage?.Project, ":", lastMessage?.Message);
                isBreak = false;
            }
            else
            {
                switch (line.ToLower())
                {
                    case "help":
                    case "?":
                        new ConsoleString(@"HELP:
    project:message
        Appends a new line to the log.
    message
        Appends a new line to the log with the previous project.
        If no previous message could be found, an error will be printed.
    break | pause
        Initializes break-mode and logs the start of the break.
        Hitting enter afterwards will end the break.
    sap
        Starts an SAP BBD ui-assisted export
    quit
    end
    exit
        Terminates execution.
    list
        Lists the logged entries of today and, if applicable, yesterday.
    list week
        Lists the logged entries of a whole week (last 7 days).
    list month
        Lists the logged entries of a whole month (last 31 days).
    undo
        Removes the previous log line.
        If the log line is not part of today, an error will be raised.")
                            {
                                Foreground = ConsoleColor.Black,
                                Background = ConsoleColor.White,
                            }
                            .WriteLine();
                        continue;
                    case "pause" when lastMessage is not null:
                    case "break" when lastMessage is not null:

                        if (line.ToLower() == "pause"
                            || line.ToLower() == "break")
                        {
                            isBreak = true;
                            line = "break:start";
                        }

                        break;
                    case "list":
                        logFiles = LoadLogFilesFromDisk(files);
                        lastMessage = PrintTodayLogFile(logFiles, undoQueue);
                        continue;
                    case "sap":
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.Write("Please enter the inclusive start date you want to modify (xx.xx): ");
                        var dateString = Console.ReadLine() ?? string.Empty;
                        Console.ResetColor();
                        DateOnly startDate;
                        try
                        {
                            startDate = DateOnly.ParseExact(
                                dateString,
                                "dd.MM",
                                CultureInfo.CurrentCulture,
                                DateTimeStyles.AllowWhiteSpaces);
                        }
                        catch (Exception ex)
                        {
                            new ConsoleString
                            {
                                Text = ex.Message + "\r\n" + ex.StackTrace,
                                Foreground = ConsoleColor.Red,
                                Background = ConsoleColor.White,
                            }.WriteLine();
                            continue;
                        }

                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.Write("Please enter the inclusive end date you want to modify (xx.xx) or leave empty for none: ");
                        DateOnly endDate;
                        dateString = Console.ReadLine() ?? string.Empty;
                        if (dateString.IsNullOrWhiteSpace())
                        {
                            endDate = DateOnly.MaxValue;
                        }
                        else
                        {
                            try
                            {
                                endDate = DateOnly.ParseExact(
                                    dateString,
                                    "dd.MM",
                                    CultureInfo.CurrentCulture,
                                    DateTimeStyles.AllowWhiteSpaces);
                            }
                            catch (Exception ex)
                            {
                                new ConsoleString
                                {
                                    Text = ex.Message + "\r\n" + ex.StackTrace,
                                    Foreground = ConsoleColor.Red,
                                    Background = ConsoleColor.White,
                                }.WriteLine();
                                continue;
                            }
                        }

                        var selectedLogFile = LoadLogFilesFromDisk(files)
                            .Where((q) => q.Date >= startDate)
                            .Where((q) => q.Date <= endDate)
                            .ToArray();
                        if (!selectedLogFile.Any())
                        {
                            new ConsoleString
                            {
                                Text = $"No log file found for date {startDate}",
                                Foreground = ConsoleColor.Red,
                                Background = ConsoleColor.White,
                            }.WriteLine();
                            continue;
                        }

                        new SapExporter("project-mapping.cfg", selectedLogFile).StartExport();
                        continue;
                    case "list week":
                        logFiles = LoadLogFilesFromDisk(files)
                            .Where((q) => (DateTime.Today - q.Date.ToDateTime(TimeOnly.MinValue)).Days <= 7)
                            .OrderBy((q) => q.Date)
                            .ToList();
                        foreach (var logFile in logFiles)
                        {
                            var tag = GetPrettyPrintTag(logFile);
                            PrintLogFile(logFile, tag);
                        }
                        continue;
                    case "list month":
                        logFiles = LoadLogFilesFromDisk(files)
                            .Where((q) => (DateTime.Today - q.Date.ToDateTime(TimeOnly.MinValue)).Days <= 31)
                            .OrderBy((q) => q.Date)
                            .ToList();
                        foreach (var logFile in logFiles)
                        {
                            var tag = GetPrettyPrintTag(logFile);
                            PrintLogFile(logFile, tag);
                        }
                        continue;
                    case "undo":
                        if (RemoveLastLineOfToday(logFiles))
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

                        continue;
                    case "end":
                    case "quit":
                    case "exit":
                        consoleCancellationTokenSource.Cancel();
                        line = "quit";
                        break;
                }
            }

            if (line.IsNullOrWhiteSpace())
            {
                new ConsoleString(
                    $"Empty line cannot be submitted.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red,
                }.WriteLine();
                continue;
            }

            var splatted = line.Split(":");
            if (splatted.Length == 1 && lastMessage is not null)
            {
                splatted = new[] {lastMessage.Project, splatted[0]};
            }

            if (splatted.Length > 1)
            {
                var now = DateTime.Now.ToUniversalTime();
                var today = now.ToDateOnly();
                var lastLogFile = logFiles.FirstOrDefault((q) => q.Date == today)
                                  ?? logFiles.AddAndReturn(
                                      new TimeLogFile(Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd")),
                                          today));
                var tmp = new TimeLogLine(now, default, splatted[0].Trim(), string.Join(":", splatted.Skip(1)).Trim());
                lastLogFile.Append(tmp);
                if (isBreak)
                {
                    PrintBreakMessage(now, now.AddMinutes(30));
                }
                else
                {
                    Print(tmp);
                    lastMessage = tmp;
                }
            }
            else
            {
                new ConsoleString(
                    $"Invalid format. Expected project to be present and separated by colon (project:description) or be 'pause'.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red,
                }.WriteLine();
            }
        }
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
        return  $"[{logFile.Date:dd.MM}]{tag}";
    }

    private static void PrintBreakMessage(DateTime from, DateTime to)
    {
        new ConsoleString(
            $"{from.ToLocalTime().ToTimeOnly()} to {to.ToLocalTime().ToTimeOnly()} -- BREAK MODE -- HIT ENTER TO CONTINUE")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.DarkGreen,
        }.WriteLine();
    }

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
        return GetOfDateOrNull(logFiles, dateOnly)
               ?? logFiles.AddAndReturn(
                   new TimeLogFile(Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd")),
                       dateOnly));
    }

    private static TimeLogFile GetOfDatePriorTo(ICollection<TimeLogFile> logFiles, DateOnly dateOnly)
    {
        return GetOfDatePriorToOrNull(logFiles, dateOnly)
               ?? logFiles.AddAndReturn(
                   new TimeLogFile(Path.Combine(Workspace, DateTime.Today.ToString("yyyy-MM-dd")),
                       dateOnly));
    }

    private static bool RemoveLastLineOfToday(List<TimeLogFile> logFiles)
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

    private static void Print(
        TimeLogLine timeLogLine,
        string? prefix = null,
        ConsoleColor? foreground = null,
        ConsoleColor? foregroundBreak = null,
        ConsoleColor? background = null,
        ConsoleColor? backgroundBreak = null)
    {
        var (timeStampStart, timeStampEnd, project, message) = timeLogLine;
        new ConsoleString(string.Concat(
            prefix ?? string.Empty,
            "[",
            timeStampStart.ToLocalTime().ToTimeOnly().ToString("HH:mm"),
            " - ",
            timeStampEnd == default
                ? "--:--"
                : timeStampEnd.ToLocalTime().ToTimeOnly().ToString("HH:mm"),
            "] ",
            project,
            ": ",
            message))
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