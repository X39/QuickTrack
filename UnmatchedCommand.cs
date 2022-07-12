namespace QuickTrack;

public record UnmatchedCommand(string Pattern, string Description, Func<string, bool> Action);