using System.Collections.Immutable;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class UndoCommand : IConsoleCommand
{
    public string[] Keys => new[] {"undo"};

    public string Description =>
        "Removes the last log line of today. " +
        "If no more log lines are available for a day to be removed, nothing will be done.";

    public string Pattern => "undo";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        if (args.Any())
        {
            new ConsoleString("Operator supports no arguments.")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        var today = await QtRepository
            .GetTodayAsync(this, cancellationToken)
            .ConfigureAwait(false);
        var logs = await today
            .GetTimeLogsAsync(cancellationToken)
            .ConfigureAwait(false);
        var lastLog = logs.MaxBy((q) => q.TimeStamp);
        if (lastLog is null)
        {
            new ConsoleString("All logs for today have been removed")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        await lastLog
            .DeleteAsync(this, cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString($"Removed {lastLog.Message}")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.DarkGreen,
        }.WriteLine();
    }
}