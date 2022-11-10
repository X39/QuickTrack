using System.Diagnostics;
using System.Runtime.CompilerServices;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using QuickTrack.Win32;
using TextCopy;
using X39.Util;
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
        // Thread.Sleep(TimeSpan.FromMilliseconds(500));
    }

    private static Instruction GatherDebugLine(string message)
        => new(string.Empty, TimeSpan.Zero, () => WriteDebugLine(message));

    protected override async ValueTask DoExportAsync(
        IEnumerable<Day> days,
        string[] args,
        CancellationToken cancellationToken)
    {
        var daysArray = days as Day[] ?? days.ToArray();
        TimeLog? mostRecentTimeLog;
        if (AppendTimeTrackingLogLine)
        {
            mostRecentTimeLog = await QtRepository.GetMostRecentNormalTimeLog(true, cancellationToken);
            var dates = daysArray
                .Select((q) => q.Date.Date.ToString("dd.MM"));
            // ReSharper disable once StringLiteralTypo
            var day = await DateTime.Today.ToDateOnly().GetDayAsync(this, cancellationToken);
            var location = Programm.Host.CurrentLocation
                           ?? await Prompt.ForLocationAsync(cancellationToken);
            var project = await Constants.ProjectForSapExport.GetProjectAsync(cancellationToken);
            await day.AppendTimeLogAsync(
                this,
                location,
                project,
                ETimeLogMode.Export,
                // ReSharper disable once StringLiteralTypo
                $"Zeiterfassung {string.Join(", ", dates)}",
                cancellationToken: cancellationToken);
        }

        await using var formatter = new ConsoleStringFormatter();

        var projectToStringMap = await MapMissingProjectsAsync(daysArray, cancellationToken)
            .ConfigureAwait(false);
        var locations = await QtRepository.GetLocationsAsync(cancellationToken)
            .ConfigureAwait(false);
        var locationsMap = locations.ToDictionary((q) => q.Id);
        await EnsureBreaksAreAddedAsync(daysArray, formatter, cancellationToken)
            .ConfigureAwait(false);
        var instructions = new List<Instruction>();
        await foreach (var instruction in GatherInstructions(
                               daysArray,
                               projectToStringMap,
                               locationsMap,
                               formatter,
                               cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            instructions.Add(instruction);
        }
        PrintTotalTimeRequired(instructions);
        PrintPreparationsHint(daysArray);
        ExecuteInstructions(instructions);
        if (AppendTimeTrackingLogLine && mostRecentTimeLog is not null)
        {
            var day = await DateTime.Today.ToDateOnly().GetDayAsync(this, cancellationToken);
            var location = await QtRepository.GetLocationAsync(mostRecentTimeLog.LocationFk, cancellationToken);
            var project = await QtRepository.GetProjectAsync(mostRecentTimeLog.ProjectFk, cancellationToken);
            await day.AppendTimeLogAsync(
                this,
                location,
                project,
                mostRecentTimeLog.Mode,
                mostRecentTimeLog.Message,
                cancellationToken: cancellationToken);
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
        var first = true;
        var stack = new Stack<Instruction>(instructions.Reverse());
        while (stack.Any())
        {
            var instruction = stack.Pop();
            if (!instruction.Source.IsNullOrEmpty())
                WriteDebugLine(instruction.Source);
            instruction.Action();


            var timeout = instruction.Timeout;
            while (timeout > TimeSpan.Zero)
            {
                if (first)
                    first = false;
                else
                    PrintRemainingTime(stack.Append(new Instruction {Timeout = timeout}), true);
                var lTimeout = timeout > TimeSpan.FromMilliseconds(250)
                    ? TimeSpan.FromMilliseconds(250)
                    : timeout;
                timeout -= lTimeout;
                PrintRemainingTime(stack.Append(new Instruction {Timeout = timeout}), false);
                Thread.Sleep(lTimeout);
            }
        }
    }

    private static TimeSpan TotalTimeRequired(IEnumerable<Instruction> instructions)
        => instructions.Aggregate(TimeSpan.Zero, (l, r) => l + r.Timeout + r.EstimatedDuration);

    private static void PrintTotalTimeRequired(IEnumerable<Instruction> instructions)
    {
        var totalTime = TotalTimeRequired(instructions);
        new ConsoleString("Export will take an estimated time of ")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Black,
        }.Write();
        new ConsoleString(totalTime.ToString())
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.Write();
        new ConsoleString(".")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }

    private static void PrintRemainingTime(IEnumerable<Instruction> instructions, bool remove)
    {
        var totalTime = TotalTimeRequired(instructions);
        var consoleString1 = new ConsoleString("Remaining time estimate: ")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Black,
        };
        var consoleString2 = new ConsoleString(totalTime.ToString())
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        };
        var consoleString3 = new ConsoleString(".")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Black,
        };
        if (remove)
        {
            var totalLength = consoleString1.Text.Length
                              + consoleString2.Text.Length
                              + consoleString3.Text.Length;
            var bsString = new string('\b', totalLength);
            var wsString = new string(' ', totalLength);
            Console.Write(bsString);
            Console.Write(wsString);
            Console.Write(bsString);
        }
        else
        {
            consoleString1.Write();
            consoleString2.Write();
            consoleString3.Write();
        }
    }

    private static void PrintPreparationsHint(IEnumerable<Day> logFiles)
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
        Log($"    6. Select the \"Aufgabe\" cell of that new row");
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
            $"Once pressing any key, a timer of {timeSpan.TotalSeconds} " +
            $"seconds will start, allowing you to focus the target area.");
        Log("Please press any key...");
        Console.ReadKey(true);
        for (var i = 5; i > 0; i--)
        {
            Log($"{i}s");
            Thread.Sleep(1000);
        }
    }

    private async Task EnsureBreaksAreAddedAsync(
        IEnumerable<Day> days,
        ConsoleStringFormatter formatter,
        CancellationToken cancellationToken)
    {
        var yieldHelper = new YieldHelperForMandatoryBreak(formatter);
        foreach (var day in days)
        {
            await foreach (var _ in yieldHelper.GetLinesWithMandatoryBreak(day, cancellationToken))
            {
                // empty
            }
        }
    }

    private async IAsyncEnumerable<(TimeLog timeLog, DateTime? end, int index)> GetLogLinesWithBreaksAdded(
        Day day,
        ConsoleStringFormatter formatter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var yieldHelper = new YieldHelperForMandatoryBreak(formatter);
        var index = 0;
        TimeLog? previous = null;
        await foreach (var timeLog in yieldHelper.GetLinesWithMandatoryBreak(day, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (previous is not null)
            {
                yield return (previous, timeLog.TimeStamp, index++);
            }

            previous = timeLog;
        }

        if (previous is not null)
        {
            yield return (previous, null, index);
        }
    }

    private async IAsyncEnumerable<Instruction> GatherInstructions(
        IReadOnlyCollection<Day> days,
        IReadOnlyDictionary<int, (string Project, string Profession, Project Entity)> projectToStringMap,
        IReadOnlyDictionary<int, Location> locationsMap,
        ConsoleStringFormatter formatter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var previousDate = days.First().Date;
        var zulu = new TimeOnly(0, 0);
        foreach (var logFile in days)
        {
            var timeSpan = previousDate.ToDateTime(zulu) - logFile.Date.ToDateTime(zulu);
            previousDate = logFile.Date;
            var totalDays = (int) Math.Round(timeSpan.TotalDays);
            if (totalDays < 0)
            {
                {
                    yield return Keyboard.Press.Escape();
                    foreach (var instruction in Keyboard.Combination.ControlPos1())
                        yield return instruction;
                    foreach (var instruction in Keyboard.Combination.ShiftTab())
                        yield return instruction;
                }
                for (var i = totalDays; i < 0; i++)
                {
                    foreach (var instruction in GatherMoveToNextDayInstructions())
                        yield return instruction;
                    yield return GatherDebugLine($"Moved to next day");
                }

                yield return Keyboard.Press.Enter();
                yield return GatherCommitTimeout();
            }

            await foreach (var instruction in GatherExportLogFileInstructions(
                                   projectToStringMap,
                                   locationsMap,
                                   logFile,
                                   formatter,
                                   cancellationToken)
                               .WithCancellation(cancellationToken))
                yield return instruction;
        }
    }

    private async IAsyncEnumerable<Instruction> GatherExportLogFileInstructions(
        IReadOnlyDictionary<int, (string Project, string Profession, Project Entity)> projectToStringMap,
        IReadOnlyDictionary<int, Location> locationsMap,
        Day day,
        ConsoleStringFormatter formatter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return GatherDebugLine($"Exporting to SAP BBD: {day.Date}");
        var logLines = GetLogLinesWithBreaksAdded(day, formatter, cancellationToken);
        await foreach (var (value, endTime, index) in logLines.WithCancellation(cancellationToken))
        {
            yield return GatherDebugLine($"Exporting Line: {value}");
            if (endTime is null)
                break;
            if (value.Mode == ETimeLogMode.Break)
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
                yield return Keyboard.Special.WriteEndTime(endTime.Value);
                yield return Keyboard.Press.Enter();
                yield return GatherCommitTimeout();
            }

            yield return Keyboard.Press.Tab();
            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteDescription(projectToStringMap[value.ProjectFk].Entity, value);
                yield return Keyboard.Press.Tab();
                yield return GatherCommitTimeout();
            }

            yield return Keyboard.Press.RightArrow();
            // ReSharper disable once StringLiteralTypo
            {
                yield return Keyboard.Press.SpaceBar();
                yield return Keyboard.Special.WriteLocation(locationsMap[value.LocationFk]);
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
            nameof(WalkToEmptyCell),
            SafetyTimeout,
            () =>
            {
                static void Invoke(Instruction instruction)
                {
                    if (!instruction.Source.IsNullOrEmpty())
                        WriteDebugLine(instruction.Source);
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
        for (var i = 0; i < 4; i++)
        {
            foreach (var instruction in Keyboard.Combination.ShiftTab())
                yield return instruction;
        }

        {
            yield return Keyboard.Press.SpaceBar();
            yield return GatherCommitTimeout();
            yield return GatherCommitTimeout();
        }

        for (var i = 0; i < 4; i++)
        {
            yield return Keyboard.Press.Tab();
        }
    }

    private static Instruction GatherCommitTimeout() => new(nameof(CommitTimeout), CommitTimeout, () => { });
    private static Instruction GatherSafetyTimeout() => new(nameof(SafetyTimeout), SafetyTimeout, () => { });

    private static class Keyboard
    {
        public static class Press
        {
            private static Instruction For(EVirtualKeyCode keyCode, [CallerMemberName] string callee = "")
                => new Instruction(
                    nameof(Press) + "." + callee,
                    SafetyTimeout,
                    () => Interop.SendKeyboardInput.KeyPress(keyCode));

            public static Instruction Escape() => For(EVirtualKeyCode.Escape);
            public static Instruction Tab() => For(EVirtualKeyCode.Tab);

            public static Instruction SpaceBar() => For(EVirtualKeyCode.SpaceBar);

            public static Instruction Enter() => For(EVirtualKeyCode.Enter);
            public static Instruction End() => For(EVirtualKeyCode.End);
            public static Instruction Pos1() => For(EVirtualKeyCode.Pos1);

            public static Instruction DownArrow() => For(EVirtualKeyCode.DownArrow);
            public static Instruction RightArrow() => For(EVirtualKeyCode.RightArrow);
            public static Instruction C() => For(EVirtualKeyCode.C);
        }

        private static class Down
        {
            private static Instruction For(EVirtualKeyCode keyCode, [CallerMemberName] string callee = "")
                => new Instruction(
                    nameof(Down) + "." + callee,
                    SafetyTimeout,
                    () => Interop.SendKeyboardInput.KeyDown(keyCode));

            public static Instruction Shift() => For(EVirtualKeyCode.Shift);
            public static Instruction Control() => For(EVirtualKeyCode.Control);
            public static Instruction LeftControl() => For(EVirtualKeyCode.LeftControl);
            public static Instruction LeftShift() => For(EVirtualKeyCode.LeftShift);
        }

        private static class Up
        {
            private static Instruction For(EVirtualKeyCode keyCode, [CallerMemberName] string callee = "")
                => new Instruction(
                    nameof(Up) + "." + callee,
                    SafetyTimeout,
                    () => Interop.SendKeyboardInput.KeyUp(keyCode));

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
                    nameof(Special) + "." + nameof(WriteText),
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
                IReadOnlyDictionary<int, (string Project, string Profession, Project Entity)> projectToStringMap,
                TimeLog timeLogLine)
            {
                var (project, _, _) = projectToStringMap[timeLogLine.ProjectFk];
                return WriteText(project);
            }

            public static Instruction WriteProfession(
                IReadOnlyDictionary<int, (string Project, string Profession, Project Entity)> projectToStringMap,
                TimeLog timeLogLine)
            {
                var (_, profession, _) = projectToStringMap[timeLogLine.ProjectFk];
                return WriteText(profession);
            }

            public static Instruction WriteEndTime(DateTime endTime)
            {
                return WriteText($"{endTime:HH:mm}");
            }


            public static Instruction WriteStartTime(TimeLog value)
            {
                return WriteText($"{value.TimeStamp:HH:mm}");
            }

            public static Instruction WriteDescription(Project project, TimeLog value)
            {
                var msg = $"{project.Title}: {value.Message}";
                return WriteText(msg);
            }

            public static Instruction WriteLocation(Location location)
            {
                return WriteText(location.Title);
            }
        }
    }

    private static async Task<Dictionary<int, (string Project, string Profession, Project Entity)>> MapMissingProjectsAsync(
        IEnumerable<Day> days,
        CancellationToken cancellationToken)
    {
        var projectToStringMap = new Dictionary<int, (string Project, string Profession, Project Entity)>();
        foreach (var day in days)
        {
            await foreach (var timeLog in day.GetTimeLogs(cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                if (projectToStringMap.ContainsKey(timeLog.ProjectFk))
                    continue;
                if (timeLog.Mode == ETimeLogMode.Break)
                    continue;
                var project = await timeLog.GetProjectAsync(cancellationToken: cancellationToken);
                var jsonAttachment = await project.GetJsonAttachment(typeof(SapBbdExport).FullName(), cancellationToken)
                    .ConfigureAwait(false);
                await jsonAttachment.WithDoAsync(
                        // ReSharper disable once VariableHidesOuterVariable
                        (JsonPayload payload, CancellationToken _) =>
                        {
                            if (payload.ProjectCode.IsNullOrWhiteSpace())
                                payload.ProjectCode = AskForProject(project);
                            if (payload.ProfessionCode.IsNullOrWhiteSpace())
                                payload.ProfessionCode = AskForProfession(project);
                            projectToStringMap[timeLog.ProjectFk] = (payload.ProjectCode, payload.ProfessionCode, project);
                            return ValueTask.CompletedTask;
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
                await jsonAttachment.UpdateAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return projectToStringMap;
    }

    public class JsonPayload
    {
        public string? ProjectCode { get; set; }
        public string? ProfessionCode { get; set; }
    }

    private static string AskForProject(Project project)
    {
        string line;
        do
        {
            new ConsoleString
            {
                // ReSharper disable once StringLiteralTypo
                Text       = $"Please input the project code to use for '{project.Title}' (eg. IPRO43-4):",
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            line = Console.ReadLine()?.Trim() ?? string.Empty;
        } while (line.IsNullOrWhiteSpace());

        var projectCode = line;
        return projectCode;
    }

    private static string AskForProfession(Project project)
    {
        string line;
        do
        {
            new ConsoleString
            {
                Text =
                    $"Please input the profession code to use for '{project.Title}' (numerical code preferred; eg. 22):",
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            line = Console.ReadLine()?.Trim() ?? string.Empty;
        } while (line.IsNullOrWhiteSpace());

        var professionCode = line;
        return professionCode;
    }

    private record struct Instruction(string Source, TimeSpan Timeout, Action Action)
    {
        public TimeSpan EstimatedDuration { get; init; } = TimeSpan.Zero;
    }
}