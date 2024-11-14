using System.Collections.Immutable;
using QuickTrack.Commands;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack;

public class QuickTrackHost
{
    public Location? CurrentLocation { get; set; }
    public Project? CurrentProject { get; set; }
    private          TimeLog?      _lastLog;
    private readonly string        _workspace;
    private          bool          _isBreak;
    private readonly Stack<string> _commandQueue = new();
    public CommandParser CommandParser { get; }

    public QuickTrackHost(
        string workspace,
        (Project Project, TimeLog TimeLog)? lastTimeLogLine,
        Location? startLocation)
    {
        _workspace = workspace;
        CommandParser = new CommandParser(
            new UnmatchedCommand(
                "project:message | message",
                "Appends a new line to the log. If project is omitted, the previous one will be used.",
                OnUnmatchedCommand));
        CommandParser.RegisterCommand(
            new FluentConsoleCommand(
                new[] {Constants.MessageForBreak, Constants.ProjectForBreak},
                () => "( pause | break ) [ Message ]",
                "Starts a break and switches time-logging to the pause mode. If a message is provided, it will be logged.",
                StartBreak));
        CommandParser.RegisterCommand(
            new FluentConsoleCommand(
                new[] {"quit", "end", "exit"},
                () => "quit | end | exit",
                "Writes 'end of day' message and terminates the program. If a message is provided, it will be logged.",
                QuitProgram));
        CommandParser.RegisterCommand(
            new FluentConsoleCommand(
                new[] {"quit-half", "end-half", "exit-half"},
                () => "quit-half | end-half | exit-half",
                "Writes OffTime event of 4h and a 'end of day' message and terminates the program. If a message is provided, it will be logged.",
                QuitHalfProgram));
        CurrentProject    = lastTimeLogLine?.Project;
        _lastLog        = lastTimeLogLine?.TimeLog;
        CurrentLocation = startLocation;
    }

    public async ValueTask QuitHalfProgram(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var message = Constants.MessageForQuit;
        if (args.Any())
            message = string.Join(" ", args);
        var day = await DateTime.Today.ToDateOnly().GetDayAsync(this, cancellationToken);
        var project = await Constants.ProjectForQuit.GetProjectAsync(cancellationToken);
        var location = CurrentLocation ?? await Prompt.ForLocationAsync(cancellationToken);
        var now = DateTime.Now;
        await day.AppendTimeLogAsync(
            this,
            location,
            project,
            ETimeLogMode.OffTime,
            message,
            timeStamp: now,
            cancellationToken: cancellationToken);
        await day.AppendTimeLogAsync(
            this,
            location,
            project,
            ETimeLogMode.Quit,
            message,
            timeStamp: now.AddHours(4),
            cancellationToken: cancellationToken);
        Programm.Quit(Constants.ErrorCodes.Ok);
    }

    public async ValueTask QuitProgram(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var message = Constants.MessageForQuit;
        if (args.Any())
            message = string.Join(" ", args);
        var day = await DateTime.Today.ToDateOnly().GetDayAsync(this, cancellationToken);
        var project = await Constants.ProjectForQuit.GetProjectAsync(cancellationToken);
        var location = CurrentLocation ?? await Prompt.ForLocationAsync(cancellationToken);
        await day.AppendTimeLogAsync(
            this,
            location,
            project,
            ETimeLogMode.Quit,
            message,
            cancellationToken: cancellationToken);
        Programm.Quit(Constants.ErrorCodes.Ok);
    }

    public async ValueTask StartBreak(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        if (_lastLog is null)
        {
            new ConsoleString("Please write a log message first.")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black
            }.WriteLine();
            return;
        }

        _isBreak = true;
        var message = Constants.MessageForBreak;
        if (args.Any())
            message = string.Join(" ", args);
        var day = await DateTime.Today.ToDateOnly().GetDayAsync(this, cancellationToken);
        var project = await Constants.ProjectForBreak.GetProjectAsync(cancellationToken);
        var location = CurrentLocation ?? await Prompt.ForLocationAsync(cancellationToken);
        var timeLog = await day.AppendTimeLogAsync(
            this,
            location,
            project,
            ETimeLogMode.Break,
            message,
            cancellationToken: cancellationToken);
        Programm.PrintBreakMessage(timeLog.TimeStamp, timeLog.TimeStamp.AddMinutes(30));
    }

    private async ValueTask<bool> OnUnmatchedCommand(string line, CancellationToken cancellationToken)
    {
        if (line.IsNullOrWhiteSpace())
        {
            if (_isBreak)
            {
                _isBreak = false;
                var lastLog = _lastLog ?? throw new NullReferenceException("_lastLog is null");
                var lastProject = CurrentProject ?? throw new NullReferenceException("CurrentProject is null");
                line = $"{lastProject.Title}:{lastLog.Message}";
            }
            else
            {
                new ConsoleString(
                    $"Empty line cannot be submitted.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red,
                }.WriteLine();
                return default;
            }
        }

        var splatted = line.Split(":");
        if (splatted.Length == 1 && CurrentProject is not null)
        {
            splatted = new[] {CurrentProject.Title, splatted[0]};
        }

        if (splatted.Length <= 1)
            return false;
        var project = splatted[0];
        var message = splatted[1];
        CurrentProject = await project.GetProjectAsync(cancellationToken);
        var day = await DateTime.Today.ToDateOnly().GetDayAsync(this, cancellationToken);
        var location = CurrentLocation ?? await Prompt.ForLocationAsync(cancellationToken);
        var tmp = _lastLog;
        _lastLog = await day.AppendTimeLogAsync(
            this,
            location,
            CurrentProject,
            ETimeLogMode.Normal,
            message,
            cancellationToken: cancellationToken);

        await using var formatter = new ConsoleStringFormatter();
        var consoleString = await _lastLog.ToConsoleString(day, formatter, cancellationToken);
        consoleString.WriteLine();
        return true;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CommandParser.PromptCommandAsync(_commandQueue, cancellationToken);
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
}