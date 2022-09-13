using System.Collections.Immutable;

namespace QuickTrack.Commands;

public interface IConsoleCommand
{
    string[] Keys { get; }
    string Description { get; }
    string Pattern { get; }

    ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken);
}