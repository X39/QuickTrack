namespace QuickTrack;

public record UnmatchedCommand(string Pattern, string Description, Func<string, CancellationToken, ValueTask<bool>> Action);