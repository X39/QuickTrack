using System.Collections.Immutable;

namespace QuickTrack.Commands;

public class LocationCommand : IConsoleCommand
{
    
    public string[] Keys { get; } = { "loc", "location" };

    public string Description { get; } =
        "Allows to change the current location, affecting the log-lines moving forward.";
    public string Pattern { get; } = "loc [ add LOCATION | list ]";
    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}