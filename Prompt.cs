using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Console;

namespace QuickTrack;

public static class Prompt
{

    #region ForLocation

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
            (location) => new ConsoleString
            {
                Text = location is null ? "[NEW LOCATION]" : location.Title,
                Foreground = ConsoleColor.Cyan,
                Background = ConsoleColor.Black
            },
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

    #endregion

    #region ForProject

    private static async Task<Project> ForProjectAsync(
        IEnumerable<Project> projects,
        CancellationToken cancellationToken)
    {
        new ConsoleString("Please choose your current project: ")
        {
            Foreground = ConsoleColor.Cyan,
            Background = ConsoleColor.Black,
        }.WriteLine();
        var chosenProject = AskConsole.ForValueFromCollection(
            projects.OrderBy((q) => q.Title).Prepend(null),
            (project) => new ConsoleString
            {
                Text = project is null ? "[NEW PROJECT]" : project.Title,
                Foreground = ConsoleColor.Cyan,
                Background = ConsoleColor.Black
            },
            new ConsoleString("Invalid selection")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            },
            cancellationToken);
        if (chosenProject is null)
        {
            string? projectName;
            do
            {
                new ConsoleString("Please provide a name for your project (select a number): ")
                {
                    Foreground = ConsoleColor.Cyan,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                projectName = Console.ReadLine();
            } while (projectName.IsNullOrWhiteSpace());

            chosenProject = await projectName.GetProjectAsync(cancellationToken);
        }

        new ConsoleString($"Chose '{chosenProject.Title}'.")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        new ConsoleString($"You can bring this selection up any time using the 'project' command.")
        {
            Foreground = ConsoleColor.Magenta,
            Background = ConsoleColor.Black,
        }.WriteLine();
        return chosenProject;
    }

    public static async Task<Project> ForProjectAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken);
        return await ForProjectAsync(projects, cancellationToken);
    }

    public static async Task<Project> ForProjectIfMultipleAsync(CancellationToken cancellationToken)
    {
        var projects = await QtRepository.GetProjectsAsync(cancellationToken)
            .ConfigureAwait(false);
        if (projects.Count == 1)
            return projects.First();
        return await ForProjectAsync(projects, cancellationToken);
    }

    #endregion
}