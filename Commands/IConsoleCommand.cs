namespace QuickTrack.Commands;

public interface IConsoleCommand
{
    string[] Keys { get; }
    string Description { get; }
    string Pattern { get; }

    void Execute(string[] args);
}