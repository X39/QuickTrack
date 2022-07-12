namespace QuickTrack.Commands;

public record FluentConsoleCommand(string[] Keys, Func<string> Pattern, string Description, Action<string[]> Action) : IConsoleCommand
{
    string IConsoleCommand.Pattern => Pattern();
    public void Execute(string[] args) => Action(args);
}