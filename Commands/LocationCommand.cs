using System.Collections.Immutable;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class LocationCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"loc", "location"};

    public string Description => "Allows to list, change, show or set the current location.";

    public string Pattern => "( loc | location ) [ list | set | edit ]";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var arg = args.FirstOrDefault();
        switch (arg)
        {
            default:
            case null:
                new ConsoleString(Programm.Host.CurrentLocation?.Title ?? "<NULL>")
                {
                    Foreground = ConsoleColor.Magenta,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                return;
            case "list":
                await ListLocationsAsync(cancellationToken);
                return;
            case "set":
                await SetLocationAsync(cancellationToken);
                return; 
            case "edit":
                await EditLocationAsync(cancellationToken);
                return; 
        }
    }

    private async Task EditLocationAsync(CancellationToken cancellationToken)
    {
        var locations = await QtRepository.GetLocationsAsync(cancellationToken)
            .ConfigureAwait(false);
        new ConsoleString("Please choose a location to edit:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var location = AskConsole.ForValueFromCollection(
            locations,
            (location) => new ConsoleString(location.Title)
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);

        string newTitle;
        do
        {
            new ConsoleString("Please enter a new name:")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
            newTitle = Console.ReadLine() ?? string.Empty;
        } while (newTitle.IsNullOrWhiteSpace());

        location.Title = newTitle;
        await location.UpdateAsync(cancellationToken);
        if (Programm.Host.CurrentLocation?.Id == location.Id)
            Programm.Host.CurrentLocation = location;
    }

    private async Task ListLocationsAsync(CancellationToken cancellationToken)
    {
        new ConsoleString("Available locations:")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        await foreach (var location in QtRepository.GetLocations(cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            new ConsoleString($"- {location.Title}")
            {
                Foreground = ConsoleColor.Magenta,
                Background = ConsoleColor.Black,
            }.WriteLine();
        }
    }

    private async Task SetLocationAsync(CancellationToken cancellationToken)
    {
        var location = await Prompt.ForLocationAsync(cancellationToken);
        Programm.Host.CurrentLocation = location;
    }
}