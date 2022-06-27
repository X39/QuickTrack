namespace QuickTrack.Commands;

public interface ICommand
{
    string[] Keys { get; }
    string Description { get; }
    string Pattern { get; }

    void Execute(string[] args);
}