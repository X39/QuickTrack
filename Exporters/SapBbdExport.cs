using System.Diagnostics;
using QuickTrack.Win32;
using TextCopy;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack.Exporters;

public class SapBbdExport : ExporterBase
{
    private static readonly TimeSpan SafetyTimeout = new(0, 0, 0, 0, 50);
    private static readonly TimeSpan CommitTimeout = new(0, 0, 0, 2, 500);
    public override string Identifier => "sap";
    protected override string ArgsPattern { get; } = string.Empty;

    public override string HelpText { get; } = "Exports (blocking) the log files " +
                                               "using a keyboard and a browser as tool.";

    [Conditional("DEBUG")]
    private static void WriteDebugLine(string message)
    {
        new ConsoleString(message)
        {
            Background = ConsoleColor.Gray,
            Foreground = ConsoleColor.White,
        }.WriteLine();
        Thread.Sleep(TimeSpan.FromMilliseconds(500));
    }

    private static Instruction GatherDebugLine(string message)
        => new(TimeSpan.Zero, () => WriteDebugLine(message));

    protected override void DoExport(IEnumerable<TimeLogFile> timeLogFiles, string[] args)
    {
        var logFiles = timeLogFiles as TimeLogFile[] ?? timeLogFiles.ToArray();
        TimeLogLine? timeTrackingLogLineReplaced;
        if (AppendTimeTrackingLogLine)
        {
            timeTrackingLogLineReplaced = Programm.GetLastLineOfToday(logFiles);
            var dates = logFiles
                .Select((q) => q.Date)
                .Select((q) => q.ToString("dd.MM"));
            // ReSharper disable once StringLiteralTypo
            Programm.Host.TryAppendNewLogLine($"SAP-BBD: Zeiterfassung {string.Join(", ", dates)}");
            if (timeTrackingLogLineReplaced is not null)
                Programm.Host.TryAppendNewLogLine(
                    $"{timeTrackingLogLineReplaced.Project}:{timeTrackingLogLineReplaced.Message}");
        }


        MapMissingProjects(logFiles, out var projectToStringMap);
        EnsureBreaksAreAdded(logFiles);
        var instructions = GatherInstructions(logFiles, projectToStringMap, ConfigHost).ToArray();
        PrintTotalTimeRequired(instructions);
        PrintPreparationsHint(logFiles);
        ExecuteInstructions(instructions);
        if (AppendTimeTrackingLogLine && timeTrackingLogLineReplaced is not null)
        {
            Programm.Host.TryAppendNewLogLine(
                $"{timeTrackingLogLineReplaced.Project}:{timeTrackingLogLineReplaced.Message}");
        }

        new ConsoleString("Done!")
        {
            Foreground = ConsoleColor.Cyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }

    private const bool AppendTimeTrackingLogLine = true;

    private static void ExecuteInstructions(IEnumerable<Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            instruction.Action();
            Thread.Sleep(instruction.Timeout);
        }
    }

    private static void PrintTotalTimeRequired(IEnumerable<Instruction> instructions)
    {
        var totalTime = instructions.Aggregate(TimeSpan.Zero, (l, r) => l + r.Timeout + r.EstimatedDuration);
        Console.WriteLine($"Export will take an estimated time of {totalTime}");
    }

    private static void PrintPreparationsHint(IEnumerable<TimeLogFile> logFiles)
    {
        void Log(string s)
            => new ConsoleString(s)
            {
                Foreground = ConsoleColor.Cyan,
                Background = ConsoleColor.Black,
            }.WriteLine();

        // ReSharper disable StringLiteralTypo
        Log($"Preparation:");
        Log($"    1. Open SAP BBD");
        Log($"    2. Navigate to \"Zeiterfassung\"");
        Log($"    3. Select the date {logFiles.First().Date}");
        Log($"    4. Choose the day view");
        Log($"    5. Create a new row");
        Log($"    6. Select the \"Aufgabe\" column");
        // ReSharper restore StringLiteralTypo

        HumanTimeout(TimeSpan.FromSeconds(5));
    }


    private static void HumanTimeout(TimeSpan timeSpan)
    {
        void Log(string s)
            => new ConsoleString(s)
            {
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.WriteLine();

        Log(
            $"Once pressing any key, a timer of {timeSpan.TotalSeconds} seconds will start to allow you focus the target area.");
        Log("Please press any key...");
        Console.ReadKey(true);
        for (var i = 5; i > 0; i--)
        {
            Log($"{i}s");
            Thread.Sleep(1000);
        }
    }

    private void EnsureBreaksAreAdded(IEnumerable<TimeLogFile> logFiles)
    {
        var yieldHelper = new YieldHelperForMandatoryBreak(ConfigHost);
        foreach (var logFile in logFiles)
        {
            yieldHelper.GetLinesWithMandatoryBreak(logFile);
        }
    }

    private static TimeLogLine[] GetLogLinesWithBreaksAdded(
        TimeLogFile logFile,
        ConfigHost configHost)
    {
        var yieldHelper = new YieldHelperForMandatoryBreak(configHost);
        return yieldHelper.GetLinesWithMandatoryBreak(logFile);
    }

    private static IEnumerable<Instruction> GatherInstructions(
        IReadOnlyCollection<TimeLogFile> timeLogFiles,
        IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
        ConfigHost configHost)
    {
        var previousDate = timeLogFiles.First().Date;
        var zulu = new TimeOnly(0, 0);
        foreach (var logFile in timeLogFiles)
        {
            var timeSpan = previousDate.ToDateTime(zulu) - logFile.Date.ToDateTime(zulu);
            previousDate = logFile.Date;
            var days = (int) Math.Round(timeSpan.TotalDays);
            for (var i = days; i < 0; i++)
            {
                foreach (var instruction in GatherMoveToNextDayInstructions())
                    yield return instruction;
                yield return GatherDebugLine($"Moved to next day");
            }

            foreach (var instruction in GatherExportLogFileInstructions(projectToStringMap, logFile, configHost))
                yield return instruction;
        }
    }

    private static IEnumerable<Instruction> GatherExportLogFileInstructions(
        IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
        TimeLogFile logFile,
        ConfigHost configHost)
    {
        yield return GatherDebugLine($"Exporting to SAP BBD: {logFile.Date}");
        var logLines = GetLogLinesWithBreaksAdded(logFile, configHost);
        foreach (var (value, index) in logLines.Indexed())
        {
            yield return GatherDebugLine($"Exporting Line: {value}");
            if (value.TimeStampEnd == default)
                break;
            if (value.Project == Constants.ProjectForBreak)
                continue;
            if (index != 0)
            {
                foreach (var instruction in GatherMoveToNextEmptyLineInstructions(index))
                    yield return instruction;
            }


            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteProject(projectToStringMap, value);
                yield return GatherCommitTimeout();
                yield return GatherCommitTimeout();
                yield return Keyboard.Press.Enter();
                yield return GatherCommitTimeout();
            }

            yield return Keyboard.Press.Tab();
            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteProfession(projectToStringMap, value);
                yield return GatherCommitTimeout();
                yield return Keyboard.Press.Enter();
                yield return GatherCommitTimeout();
            }

            yield return Keyboard.Press.Tab();
            yield return Keyboard.Press.Tab();
            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteStartTime(value);
                yield return Keyboard.Press.Enter();
                yield return GatherCommitTimeout();
            }

            yield return Keyboard.Press.Tab();
            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteEndTime(value);
                yield return Keyboard.Press.Enter();
                yield return GatherCommitTimeout();
            }

            yield return Keyboard.Press.Tab();
            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteDescription(value);
                yield return Keyboard.Press.Tab();
                yield return GatherCommitTimeout();
            }
        }
    }

    private static IEnumerable<Instruction> GatherMoveToNextEmptyLineInstructions(int index)
    {
        {
            foreach (var instruction in Keyboard.Combination.ControlEnd())
                yield return instruction;
            yield return Keyboard.Press.Enter();
            yield return GatherCommitTimeout();
            yield return GatherCommitTimeout();
            yield return Keyboard.Press.Escape();
        }

        {
            foreach (var instruction in Keyboard.Combination.ControlPos1())
                yield return instruction;
        }

        yield return WalkToEmptyCell(index);

        yield return Keyboard.Press.Enter();
        yield return GatherCommitTimeout();
    }

    private static Instruction WalkToEmptyCell(int index)
        => new(
            SafetyTimeout,
            () =>
            {
                static void Invoke(Instruction instruction)
                {
                    instruction.Action();
                    Thread.Sleep(instruction.Timeout);
                }

                bool IsCellEmpty()
                {
                    var existingText = ClipboardService.GetText();
                    ClipboardService.SetText(string.Empty);
                    Invoke(Keyboard.Press.SpaceBar());
                    Invoke(Keyboard.Press.Pos1());
                    foreach (var instruction in Keyboard.Combination.ShiftEnd())
                        Invoke(instruction);

                    foreach (var instruction in Keyboard.Combination.ControlC())
                        Invoke(instruction);

                    Invoke(Keyboard.Press.Escape());
                    var clipboardText = ClipboardService.GetText();
                    ClipboardService.SetText(existingText ?? string.Empty);
                    return clipboardText.IsNullOrWhiteSpace();
                }

                for (var i = 0; i < index + 2; i++)
                {
                    if (i > 0)
                        Invoke(Keyboard.Press.DownArrow());
                    Invoke(GatherSafetyTimeout());
                    if (IsCellEmpty())
                        break;
                }
            })
        {
            EstimatedDuration = SafetyTimeout * (index + 2) * 11,
        };

    private static IEnumerable<Instruction> GatherMoveToNextDayInstructions()
    {
        {
            yield return Keyboard.Press.Escape();
            foreach (var instruction in Keyboard.Combination.ShiftTab())
                yield return instruction;
        }

        {
            foreach (var instruction in Keyboard.Combination.ShiftTab())
                yield return instruction;
            foreach (var instruction in Keyboard.Combination.ShiftTab())
                yield return instruction;
            foreach (var instruction in Keyboard.Combination.ShiftTab())
                yield return instruction;
            foreach (var instruction in Keyboard.Combination.ShiftTab())
                yield return instruction;
        }

        {
            yield return Keyboard.Press.SpaceBar();
            yield return GatherCommitTimeout();
            yield return GatherCommitTimeout();
        }

        {
            yield return Keyboard.Press.Tab();
            yield return Keyboard.Press.Tab();
            yield return Keyboard.Press.Tab();
            yield return Keyboard.Press.Tab();
            yield return Keyboard.Press.Enter();
        }
    }

    private static Instruction GatherCommitTimeout() => new(CommitTimeout, () => { });
    private static Instruction GatherSafetyTimeout() => new(SafetyTimeout, () => { });

    private static class Keyboard
    {
        public static class Press
        {
            private static Instruction For(EVirtualKeyCode keyCode)
                => new Instruction(SafetyTimeout, () => Interop.SendKeyboardInput.KeyPress(keyCode));

            public static Instruction Escape() => For(EVirtualKeyCode.Escape);
            public static Instruction Tab() => For(EVirtualKeyCode.Tab);

            public static Instruction SpaceBar() => For(EVirtualKeyCode.SpaceBar);

            public static Instruction Enter() => For(EVirtualKeyCode.Enter);
            public static Instruction End() => For(EVirtualKeyCode.End);
            public static Instruction Pos1() => For(EVirtualKeyCode.Pos1);

            public static Instruction DownArrow() => For(EVirtualKeyCode.DownArrow);
            public static Instruction C() => For(EVirtualKeyCode.C);
        }

        private static class Down
        {
            private static Instruction For(EVirtualKeyCode keyCode)
                => new Instruction(SafetyTimeout, () => Interop.SendKeyboardInput.KeyDown(keyCode));

            public static Instruction Shift() => For(EVirtualKeyCode.Shift);
            public static Instruction Control() => For(EVirtualKeyCode.Control);
            public static Instruction LeftControl() => For(EVirtualKeyCode.LeftControl);
            public static Instruction LeftShift() => For(EVirtualKeyCode.LeftShift);
        }

        private static class Up
        {
            private static Instruction For(EVirtualKeyCode keyCode)
                => new Instruction(SafetyTimeout, () => Interop.SendKeyboardInput.KeyUp(keyCode));

            public static Instruction Shift() => For(EVirtualKeyCode.Shift);
            public static Instruction Control() => For(EVirtualKeyCode.Control);
            public static Instruction LeftControl() => For(EVirtualKeyCode.LeftControl);
            public static Instruction LeftShift() => For(EVirtualKeyCode.LeftShift);
        }

        public static class Combination
        {
            public static IEnumerable<Instruction> ShiftTab()
            {
                yield return Down.LeftShift();
                yield return Press.Tab();
                yield return Up.LeftShift();
            }

            public static IEnumerable<Instruction> ControlEnd()
            {
                yield return Down.Control();
                yield return Press.End();
                yield return Up.Control();
            }

            public static IEnumerable<Instruction> ControlPos1()
            {
                yield return Down.Control();
                yield return Press.Pos1();
                yield return Up.Control();
            }

            public static IEnumerable<Instruction> ShiftEnd()
            {
                yield return Down.Shift();
                yield return Press.End();
                yield return Up.Shift();
            }

            public static IEnumerable<Instruction> ControlC()
            {
                yield return Down.LeftControl();
                yield return Press.C();
                yield return Up.LeftControl();
            }
        }

        public static class Special
        {
            private static Instruction WriteText(string text)
            {
                return new Instruction(
                    SafetyTimeout,
                    () =>
                    {
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
                    });
            }

            public static Instruction WriteProject(
                IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
                TimeLogLine timeLogLine)
            {
                var (project, _) = projectToStringMap[timeLogLine.Project.Trim()];
                return WriteText(project);
            }

            public static Instruction WriteProfession(
                IReadOnlyDictionary<string, (string Project, string Profession)> projectToStringMap,
                TimeLogLine timeLogLine)
            {
                var (_, profession) = projectToStringMap[timeLogLine.Project.Trim()];
                return WriteText(profession);
            }

            public static Instruction WriteEndTime(TimeLogLine value)
            {
                return WriteText($"{value.TimeStampEnd:HH:mm}");
            }


            public static Instruction WriteStartTime(TimeLogLine value)
            {
                return WriteText($"{value.TimeStampStart:HH:mm}");
            }

            public static Instruction WriteDescription(TimeLogLine value)
            {
                var msg = $"{value.Project}: {value.Message}";
                return WriteText(msg);
            }
        }
    }

    private void MapMissingProjects(
        IEnumerable<TimeLogFile> logFiles,
        out Dictionary<string, (string Project, string Profession)> projectToStringMap)
    {
        projectToStringMap = new Dictionary<string, (string Project, string Profession)>();
        foreach (var timeLogLine in logFiles.SelectMany((q) => q.GetLines()))
        {
            if (projectToStringMap.ContainsKey(timeLogLine.Project.Trim()))
                continue;
            if (timeLogLine.Project == Constants.ProjectForBreak)
                continue;
            var key = $"Mapping@{timeLogLine.Project}";
            var value = ConfigHost.Get<ProjectProfessionTuple>(
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
                // ReSharper disable once StringLiteralTypo
                Text       = $"Please input the project code to use for '{timeLogLine.Project}' (eg. IPRO43-4):",
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
                    $"Please input the profession code to use for '{timeLogLine.Project}' (numerical code preferred; eg. 22):",
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            line = Console.ReadLine()?.Trim() ?? string.Empty;
            if (line.IsNullOrWhiteSpace())
                goto askProfessionCode;

            projectToStringMap[timeLogLine.Project.Trim()] = (project, line);
            ConfigHost.Set(
                typeof(SapExporter).FullName(),
                key,
                new ProjectProfessionTuple(project, line));
        }
    }

    private record ProjectProfessionTuple(string Project, string Profession);

    private record struct Instruction(TimeSpan Timeout, Action Action)
    {
        public TimeSpan EstimatedDuration { get; init; } = TimeSpan.Zero;
    }

    public SapBbdExport(ConfigHost configHost) : base(configHost)
    {
    }
}