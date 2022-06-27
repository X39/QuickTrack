namespace QuickTrack.Commands;

public record FluentCommand(string[] Keys, Func<string> Pattern, string Description, Action<string[]> Action) : ICommand
{
    string ICommand.Pattern => Pattern();
    public void Execute(string[] args) => Action(args);
}