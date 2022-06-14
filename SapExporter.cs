using QuickTrack.Win32;
using TextCopy;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack;

public class SapExporter
{
    private readonly TimeLogFile[] _logFiles;
    private int _logDepth = 0;
    private Dictionary<TimeLogFile, TimeLogLine[]> _logLineCache = new();
    private readonly ConfigHost _configHost;

    private enum ETimeout
    {
        Safety,
        Commit,
    }

    private static TimeSpan Get(ETimeout timeout)
    {
        const int confirmTimeoutMs = 2500; // 2000
        const int safetyTimeoutMs = 50; // 50
        return timeout switch
        {
            ETimeout.Safety => TimeSpan.FromMilliseconds(safetyTimeoutMs),
            ETimeout.Commit => TimeSpan.FromMilliseconds(confirmTimeoutMs),
            _ => throw new ArgumentOutOfRangeException(nameof(timeout), timeout, null)
        };
    }

    public SapExporter(ConfigHost configHost, params TimeLogFile[] logFiles)
    {
        _logFiles = logFiles;
        _configHost = configHost;
    }

    private IDisposable LogScope()
    {
        return new Disposable(
            () => _logDepth++,
            () => _logDepth--);
    }

    private void LogInfo(string s)
    {
        new ConsoleString
        {
            Text = string.Concat(new string(' ', _logDepth * 4), s),
            Foreground = ConsoleColor.Cyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }

    private void LogDebug(string s)
    {
#if DEBUG
        new ConsoleString
        {
            Text = string.Concat(new string(' ', _logDepth * 4), s),
            Foreground = ConsoleColor.Gray,
            Background = ConsoleColor.Black,
        }.WriteLine();
#endif
    }

    private void EnterProject(IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
        TimeLogLine timeLogLine)
    {
        var (project, _) = projectToStringMap[timeLogLine.Project.Trim()];
        LogDebug($"EnterProject('{timeLogLine.Project}' --> '{project}')");
        EnterText(project);
    }

    private void EnterProfession(IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
        TimeLogLine timeLogLine)
    {
        var (_, profession) = projectToStringMap[timeLogLine.Project.Trim()];
        LogDebug($"EnterProfession('{timeLogLine.Project}' --> '{profession}')");
        EnterText(profession);
    }

    private void WaitFor(TimeSpan timeSpan)
    {
        LogDebug($"WaitFor({timeSpan.TotalMilliseconds}ms)");
        Thread.Sleep(timeSpan);
    }

    private void EnterEndTime(TimeLogLine value)
    {
        LogDebug($"EnterEndTime({value.TimeStampEnd:HH:mm})");
        EnterText($"{value.TimeStampEnd:HH:mm}");
    }


    private void EnterStartTime(TimeLogLine value)
    {
        LogDebug($"EnterStartTime({value.TimeStampStart:HH:mm})");
        EnterText($"{value.TimeStampStart:HH:mm}");
    }

    private void EnterDescription(TimeLogLine value)
    {
        var msg = $"{value.Project}: {value.Message}";
        LogDebug($"EnterDescription({msg})");
        EnterText(msg);
    }

    private void EnterText(string text)
    {
        LogDebug($"EnterText({text})");
        foreach (var c in text)
        {
            switch (c)
            {
                case >= 'a' and <= 'z':
                    Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.A + (c - 'a'));
                    break;
                case >= 'A' and <= 'Z':
                    Interop.SendKeyboardInput.KeyDown(EVirtualKeyCode.LeftShift);
                    Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.A + (c - 'A'));
                    Interop.SendKeyboardInput.KeyUp(EVirtualKeyCode.LeftShift);
                    break;
                case >= '0' and <= '9':
                    Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.Key0 + (c - '0'));
                    break;
                default:
                    Interop.SendKeyboardInput.Char(c);
                    break;
            }
        }
    }

    public void StartExport()
    {
        LogInfo("Preparation:");
        LogInfo("    1. Open SAP BBD");
        // ReSharper disable once StringLiteralTypo
        LogInfo("    2. Navigate to \"Zeiterfassung\"");
        LogInfo($"    3. Select the date {_logFiles.First().Date}");
        // ReSharper disable once StringLiteralTypo
        LogInfo("    4. DO NOT SORT! If you sorted, reopen the browser tab.");
        LogInfo("    5. Create a new row");
        // ReSharper disable once StringLiteralTypo
        LogInfo("    6. Select the \"Aufgabe\" column");
        LogInfo("Please hit any once done...");
        Console.ReadKey(true);

        var projectToStringMap = GetProjectToStringMap();

        foreach (var logFile in _logFiles)
        {
            _ = GetLogLines(logFile, forceTimeout: false);
        }


        Timeout();


        var previousDate = _logFiles.First().Date;
        var zulu = new TimeOnly(0, 0);
        foreach (var logFile in _logFiles)
        {
            var timeSpan = previousDate.ToDateTime(zulu) - logFile.Date.ToDateTime(zulu);
            previousDate = logFile.Date;
            var days = (int) Math.Round(timeSpan.TotalDays);
            for (var i = days; i < 0; i++)
            {
                MoveToNextDay();
            }

            ExportLogFile(projectToStringMap, logFile);
        }
    }

    private void Timeout()
    {
        LogInfo("Once pressing any key, a timer of 5 seconds will start to allow you focus the target area.");
        LogInfo("Please press any key...");
        Console.ReadKey(true);
        for (var i = 5; i > 0; i--)
        {
            LogInfo($"{i}s");
            Thread.Sleep(1000);
        }
    }

    private Dictionary<string, (string Project, string Profession)> GetProjectToStringMap()
    {
        var dict = new Dictionary<string, (string Project, string Profession)>();
        MapMissingProjects(dict);
        return dict;
    }


    private record ProjectProfessionTuple(string Project, string Profession);
    private void MapMissingProjects(IDictionary<string, (string Project, string Profession)> projectToStringMap)
    {
        foreach (var timeLogLine in _logFiles.SelectMany((q) => q.GetLines()))
        {
            if (projectToStringMap.ContainsKey(timeLogLine.Project.Trim()))
                continue;
            if (timeLogLine.Project == "break")
                continue;
            var key = $"Mapping@{timeLogLine.Project}";
            var value = _configHost.Get<ProjectProfessionTuple>(
                typeof(SapExporter).FullName(),
                key);
            if (value is not null)
            {
                projectToStringMap[timeLogLine.Project] = (value.Project, value.Profession);
                continue;
            }

            askProjectCode:
            new ConsoleString
            {
                Text = $"Please input the project code to use for '{timeLogLine.Project}':",
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            var line = Console.ReadLine()?.Trim() ?? string.Empty;
            if (line.IsNullOrWhiteSpace())
                goto askProjectCode;
            var project = line;


            askProfessionCode:
            new ConsoleString
            {
                Text =
                    $"Please input the profession code to use for '{timeLogLine.Project}' (numerical code preferred):",
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            line = Console.ReadLine()?.Trim() ?? string.Empty;
            if (line.IsNullOrWhiteSpace())
                goto askProfessionCode;

            projectToStringMap[timeLogLine.Project.Trim()] = (project, line);
            _configHost.Set(
                typeof(SapExporter).FullName(),
                key,
                new ProjectProfessionTuple(project, line));
        }
    }

    private void MoveToNextDay()
    {
        LogInfo("Leaving text field");
        using (LogScope())
        {
            PressEscape();
            WaitFor(Get(ETimeout.Safety));
            PressShiftTab();
            WaitFor(Get(ETimeout.Safety));
        }

        LogInfo("Navigating to next day button");
        using (LogScope())
        {
            PressShiftTab();
            WaitFor(Get(ETimeout.Safety));
            PressShiftTab();
            WaitFor(Get(ETimeout.Safety));
            PressShiftTab();
            WaitFor(Get(ETimeout.Safety));
            PressShiftTab();
            WaitFor(Get(ETimeout.Safety));
        }

        LogInfo("Pressing next day button");
        using (LogScope())
        {
            PressSpaceBar();
            WaitFor(Get(ETimeout.Commit));
            WaitFor(Get(ETimeout.Commit));
        }

        LogInfo("Navigating back into text area");
        using (LogScope())
        {
            PressTab();
            WaitFor(Get(ETimeout.Safety));
            PressTab();
            WaitFor(Get(ETimeout.Safety));
            PressTab();
            WaitFor(Get(ETimeout.Safety));
            PressTab();
            WaitFor(Get(ETimeout.Safety));
            PressEnter();
            WaitFor(Get(ETimeout.Commit));
        }
    }

    private void ExportLogFile(
        IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
        TimeLogFile logFile)
    {
        LogInfo("Starting export...");

        var logLines = GetLogLines(logFile);
        foreach (var (value, index) in logLines.Indexed())
        {
            LogInfo(value.ToString());
            using var logLineScope = LogScope();
            if (value.TimeStampEnd == default)
                break;
            if (value.Project == "break")
                continue;
            if (index != 0)
            {
                LogInfo("Moving to empty line");
                using var moveToEmptyLineScope = LogScope();

                LogInfo("Navigate out of cell");
                using (LogScope())
                {
                    HoldControl();
                    WaitFor(Get(ETimeout.Safety));
                    PressEnd();
                    WaitFor(Get(ETimeout.Safety));
                    ReleaseControl();
                    WaitFor(Get(ETimeout.Safety));
                    PressEnter();
                    WaitFor(Get(ETimeout.Commit));
                    WaitFor(Get(ETimeout.Commit));
                    PressEscape();
                    WaitFor(Get(ETimeout.Safety));
                }

                LogInfo("Move to top-left corner");
                using (LogScope())
                {
                    HoldControl();
                    PressPos1();
                    ReleaseControl();
                }

                LogInfo("Walk down to empty cell");
                using (LogScope())
                {
                    for (var i = 0; i < index + 2; i++)
                    {
                        if (i > 0)
                            PressDownArrow();
                        WaitFor(Get(ETimeout.Safety));
                        if (IsCellEmpty())
                        {
                            LogDebug("Cell is empty");
                            break;
                        }

                        LogDebug("Cell not empty");
                        WaitFor(Get(ETimeout.Safety));
                    }
                }

                PressEnter();
                WaitFor(TimeSpan.FromSeconds(3));
            }


            // ReSharper disable once StringLiteralTypo
            LogInfo("Entering 'Aufgabe' (Project)");
            using (LogScope())
            {
                EnsureField();
                WaitFor(Get(ETimeout.Safety));
                EnterProject(projectToStringMap, value);
                WaitFor(Get(ETimeout.Commit));
                WaitFor(Get(ETimeout.Commit));
                PressEnter();
                WaitFor(Get(ETimeout.Commit));
            }

            PressTab();
            // ReSharper disable once StringLiteralTypo
            LogInfo("Entering 'Tätigkeit' (Profession)");
            using (LogScope())
            {
                EnsureField();
                WaitFor(Get(ETimeout.Safety));
                EnterProfession(projectToStringMap, value);
                WaitFor(Get(ETimeout.Commit));
                PressEnter();
                WaitFor(Get(ETimeout.Commit));
            }

            PressTab();
            WaitFor(Get(ETimeout.Safety));
            PressTab();
            // ReSharper disable once StringLiteralTypo
            LogInfo("Entering 'Beginn' (StartTime)");
            using (LogScope())
            {
                EnsureField();
                WaitFor(Get(ETimeout.Safety));
                EnterStartTime(value);
                WaitFor(Get(ETimeout.Safety));
                PressEnter();
                WaitFor(Get(ETimeout.Commit));
            }

            PressTab();
            WaitFor(Get(ETimeout.Safety));
            // ReSharper disable once StringLiteralTypo
            LogInfo("Entering 'Ende' (StartTime)");
            using (LogScope())
            {
                EnsureField();
                WaitFor(Get(ETimeout.Safety));
                EnterEndTime(value);
                WaitFor(Get(ETimeout.Safety));
                PressEnter();
                WaitFor(Get(ETimeout.Commit));
            }

            PressTab();
            WaitFor(Get(ETimeout.Safety));
            // ReSharper disable once StringLiteralTypo
            LogInfo("Entering 'Arbeitsbeschreibung' (Description)");
            using (LogScope())
            {
                EnsureField();
                WaitFor(Get(ETimeout.Safety));
                EnterDescription(value);
                WaitFor(Get(ETimeout.Safety));
                PressTab();
                WaitFor(Get(ETimeout.Commit));
            }
        }
    }

    private IEnumerable<TimeLogLine> GetLogLines(TimeLogFile logFile, bool forceTimeout = true)
    {
        if (_logLineCache.TryGetValue(logFile, out var logLines))
            return logLines;
        var lines = logFile.GetLines().ToArray();
        var totalPause = lines.Aggregate(
            TimeSpan.Zero, (l, r) => l + (r.Project == "break" ? r.TimeStampEnd - r.TimeStampStart : TimeSpan.Zero));
        if (totalPause >= TimeSpan.FromMinutes(30))
            return lines;
        var delta = TimeSpan.FromMinutes(30) - totalPause;
        Console.WriteLine($"Missing mandatory break time on {logFile.Date.ToLongDateString()}.");
        Console.WriteLine($"Got {totalPause} of break time, expected at least {TimeSpan.FromMinutes(30)}.");
        Console.WriteLine($"Please choose where to append {delta} of mandatory break time.");
        Console.WriteLine($"Time will be appended AFTER the selected entry.");
        var line = AskConsole.ForValueFromCollection(lines, (q) => new ConsoleString
        {
            Text = q.ToString(),
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Blue,
        });
        if (forceTimeout)
            Timeout();
        var pre = lines.TakeWhile((q) => q != line);
        var end = lines.SkipWhile((q) => q != line && pre.Contains(q)).Skip(1);
        return _logLineCache[logFile] = pre
            .Append(line)
            .Append(new TimeLogLine(line.TimeStampEnd, line.TimeStampEnd + delta, "break", "pause"))
            .Concat(end.Select((q) => q with
            {
                TimeStampStart = q.TimeStampStart + delta,
                TimeStampEnd = q.TimeStampEnd == default ? default : q.TimeStampEnd + delta,
            })).ToArray();
    }


    #region Combo press/release/hold methods

    private void EnsureField()
    {
        LogDebug("EnsureField()");
        using var logScope = LogScope();
        PressSpaceBar();
        WaitFor(Get(ETimeout.Safety));
        // PressRightArrow();
        // WaitFor(Get(ETimeout.Safety));
        // PressLeftArrow();
        // WaitFor(Get(ETimeout.Safety));
        // PressBackspace();
    }

    private void PressShiftTab()
    {
        LogDebug("PressShiftTab()");
        using var logScope = LogScope();
        HoldLeftShift();
        WaitFor(Get(ETimeout.Safety));
        PressTab();
        WaitFor(Get(ETimeout.Safety));
        ReleaseLeftShift();
    }

    private bool IsCellEmpty()
    {
        var existingText = ClipboardService.GetText();
        ClipboardService.SetText(string.Empty);
        LogDebug("IsCellEmpty()");
        using var logScope = LogScope();
        EnsureField();
        WaitFor(Get(ETimeout.Safety));
        PressPos1();
        WaitFor(Get(ETimeout.Safety));
        SelectToEnd();
        WaitFor(Get(ETimeout.Safety));
        PressCtrlC();
        WaitFor(Get(ETimeout.Safety));
        PressEscape();
        var clipboardText = ClipboardService.GetText();
        ClipboardService.SetText(existingText ?? string.Empty);
        return clipboardText.IsNullOrWhiteSpace();
    }

    private void SelectToEnd()
    {
        LogDebug("SelectToEnd()");
        using var logScope = LogScope();
        HoldShift();
        WaitFor(Get(ETimeout.Safety));
        PressEnd();
        WaitFor(Get(ETimeout.Safety));
        ReleaseShift();
    }

    private void PressCtrlC()
    {
        LogDebug("PressCtrlC()");
        using var logScope = LogScope();
        HoldLeftControl();
        WaitFor(Get(ETimeout.Safety));
        PressC();
        WaitFor(Get(ETimeout.Safety));
        ReleaseLeftControl();
    }

    #endregion


    #region Single press/release/hold methods

    private void PressEnd()
    {
        LogDebug("PressEnd()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.End);
    }

    private void PressTab()
    {
        LogDebug("PressTab()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.Tab);
    }

    private void PressEnter()
    {
        LogDebug("PressEnter()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.Enter);
    }

    private void PressDownArrow()
    {
        LogDebug("PressDownArrow()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.DownArrow);
    }

    private void PressRightArrow()
    {
        LogDebug("PressRightArrow()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.RightArrow);
    }

    private void PressUpArrow()
    {
        LogDebug("PressUpArrow()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.UpArrow);
    }

    private void PressLeftArrow()
    {
        LogDebug("PressLeftArrow()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.LeftArrow);
    }

    private void PressPos1()
    {
        LogDebug("PressPos1()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.Pos1);
    }

    private void PressC()
    {
        LogDebug("PressC()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.C);
    }

    private void ReleaseLeftControl()
    {
        LogDebug("ReleaseLeftControl()");
        Interop.SendKeyboardInput.KeyUp(EVirtualKeyCode.LeftControl);
    }

    private void ReleaseControl()
    {
        LogDebug("ReleaseControl()");
        Interop.SendKeyboardInput.KeyUp(EVirtualKeyCode.Control);
    }

    private void HoldLeftControl()
    {
        LogDebug("HoldLeftControl()");
        Interop.SendKeyboardInput.KeyDown(EVirtualKeyCode.LeftControl);
    }


    private void HoldControl()
    {
        LogDebug("HoldControl()");
        Interop.SendKeyboardInput.KeyDown(EVirtualKeyCode.Control);
    }

    private void ReleaseLeftShift()
    {
        LogDebug("ReleaseLeftShift()");
        Interop.SendKeyboardInput.KeyUp(EVirtualKeyCode.LeftShift);
    }

    private void ReleaseShift()
    {
        LogDebug("ReleaseShift()");
        Interop.SendKeyboardInput.KeyUp(EVirtualKeyCode.Shift);
    }

    private void HoldLeftShift()
    {
        LogDebug("HoldLeftShift()");
        Interop.SendKeyboardInput.KeyDown(EVirtualKeyCode.LeftShift);
    }

    private void HoldShift()
    {
        LogDebug("HoldShift()");
        Interop.SendKeyboardInput.KeyDown(EVirtualKeyCode.Shift);
    }

    private void PressSpaceBar()
    {
        LogDebug("PressSpaceBar()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.SpaceBar);
    }

    private void PressBackspace()
    {
        LogDebug("PressBackspace()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.Backspace);
    }

    private void PressEscape()
    {
        LogDebug("PressEscape()");
        Interop.SendKeyboardInput.KeyPress(EVirtualKeyCode.Escape);
    }

    #endregion

    public static bool WriteLineByDefault(ConfigHost configHost)
    {
        return configHost.Get(typeof(SapExporter).FullName(), "write-line", true);
    }
    public static void WriteLineByDefault(ConfigHost configHost, bool value)
    {
        configHost.Set(typeof(SapExporter).FullName(), "write-line", value);
    }
}