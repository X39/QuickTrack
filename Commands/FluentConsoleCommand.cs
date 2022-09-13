using System.Collections.Immutable;

namespace QuickTrack.Commands;

public record FluentConsoleCommand(
    string[] Keys,
    Func<string> Pattern,
    string Description,
    Func<ImmutableArray<string>, CancellationToken, ValueTask> Action) : IConsoleCommand
{
    public FluentConsoleCommand(
        string[] keys,
        Func<string> pattern,
        string description,
        Func<ImmutableArray<string>, ValueTask> action)
        : this(
            keys,
            pattern,
            description,
            (arg, _) =>
            {
                action(arg);
                return ValueTask.CompletedTask;
            })
    {
    }

    string IConsoleCommand.Pattern => Pattern();

    public ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        return Action(args, cancellationToken);
    }
}