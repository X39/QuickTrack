using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack;

public static class Prompt
{
    private static async Task<Location> ForLocationAsync(
        IEnumerable<Location> locations,
        CancellationToken cancellationToken)
    {
        new ConsoleString("Please choose your current location: ")
        {
            Foreground = ConsoleColor.Cyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var chosenLocation = AskConsole.ForValueFromCollection(
            locations.OrderBy((q) => q.Title).Prepend(null),
            (location) => location is null ? "[NEW LOCATION]" : location.Title,
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);
        if (chosenLocation is null)
        {
            string? locationName;
            do
            {
                new ConsoleString("Please provide a name for your location (select a number): ")
                {
                    Foreground = ConsoleColor.Cyan,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                locationName = Console.ReadLine();
            } while (locationName.IsNullOrWhiteSpace());

            chosenLocation = await locationName.GetLocationAsync(cancellationToken);
        }

        new ConsoleString($"Chose '{chosenLocation.Title}'.")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        new ConsoleString($"You can bring this selection up any time using the 'location' command.")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        return chosenLocation;
    }

    public static async Task<Location> ForLocationAsync(CancellationToken cancellationToken)
    {
        var locations = await QtRepository.GetLocationsAsync(cancellationToken);
        return await ForLocationAsync(locations, cancellationToken);
    }

    public static async Task<Location> ForLocationIfMultipleAsync(CancellationToken cancellationToken)
    {
        var locations = await QtRepository.GetLocationsAsync(cancellationToken)
            .ConfigureAwait(false);
        if (locations.Count == 1)
            return locations.First();
        return await ForLocationAsync(locations, cancellationToken);
    }
}