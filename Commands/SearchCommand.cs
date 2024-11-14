using System.Collections.Immutable;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class SearchCommand : IConsoleCommand
{
    public virtual string[] Keys { get; } =
    {
        "search"
    };

    public virtual string Description => "Searches the given number of log-files for the given TEXT. "
                                         + "If no argument is provided, only today and yesterday will be listed. "
                                         + "If * is provided, all log-files will be searched. "
                                         + "If week is provided, the previous 7 days will be listed. "
                                         + "If month is provided, the previous 31 days will be listed. "
                                         + "If a number is provided, the previous X days will be listed where X is the number and 1 is today.";

    // ReSharper disable once StringLiteralTypo
    public virtual string Pattern => "search [ NUMBEROFDAYS | week | month ] TEXT...";

    public async virtual ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var separators = new[]
        {
            ' ',
            '[',
            ']',
            '|',
            ':'
        };
        var (days, text) = ParseArguments(args);
        var locations = await GetMatchingLocationsAsync(text, cancellationToken);
        var projects = await GetMatchingProjectsAsync(text, cancellationToken);
        await PrintDaysFoundAsync(days, locations, projects, [], separators, text, cancellationToken);
    }
    protected async Task PrintDaysFoundAsync(
        int days,
        IReadOnlyCollection<Location> locations,
        IReadOnlyCollection<Project> projectsToFind,
        IReadOnlyCollection<Project> projectsToSearch,
        char[] separators,
        ImmutableArray<string> text,
        CancellationToken cancellationToken
    )
    {
        await using var consoleStringFormatter = new ConsoleStringFormatter();
        await foreach (var day in QtRepository.GetDays(cancellationToken).WithCancellation(cancellationToken))
        {
            if (days-- <= 0)
                return;
            await foreach (var timeLog in day.GetTimeLogs(cancellationToken).WithCancellation(cancellationToken))
            {
                if (projectsToSearch.Count != 0 && projectsToSearch.All(q => q.Id != timeLog.ProjectFk))
                    continue;
                var match = default(bool?);
                if (locations.Any((q) => q.Id == timeLog.LocationFk))
                    match = (match ?? true) || true;
                if (projectsToFind.Any((q) => q.Id == timeLog.ProjectFk))
                    match = (match ?? true) || true;
                if (MatchesByDistance(timeLog.Message.Split(separators), text))
                    match = (match ?? true) || true;
                if (match is null or false)
                    continue;
                var consoleString = await timeLog.ToConsoleString(day, consoleStringFormatter, cancellationToken);
                consoleString.WriteLine();
            }
        }
    }
    protected static (int days, ImmutableArray<string> text) ParseArguments(
        ImmutableArray<string> args,
        char[]? separators = null
    )
    {
        separators ??= new[]
        {
            ' ',
            '[',
            ']',
            '|',
            ':'
        };

        var days = int.MaxValue;
        if (args.Length > 1)
        {
            var first = args.First().ToLower().Trim();
            days = first switch
            {
                "week"  => 7,
                "month" => 31,
                "*"     => int.MaxValue,
                {
                } when first.All(char.IsDigit) => int.Parse(first),
                _ => 1,
            } - 1;
        }

        var text = args
                   .Skip(1)
                   .SelectMany((arg) => arg.Split(separators, StringSplitOptions.RemoveEmptyEntries))
                   .ToImmutableArray();
        return (days, text);
    }

    private async Task<IReadOnlyCollection<Location>> GetMatchingLocationsAsync(
        ImmutableArray<string> text,
        CancellationToken cancellationToken
    )
    {
        var locations = new List<Location>();
        foreach (var s in text)
        {
            var location = await QtRepository.GetLocationOrDefaultAsync(s, cancellationToken).ConfigureAwait(false);
            if (location is null)
                continue;
            locations.Add(location);
        }

        return locations;
    }


    protected async Task<IReadOnlyCollection<Project>> GetMatchingProjectsAsync(
        ImmutableArray<string> text,
        CancellationToken cancellationToken
    )
    {
        var projects = new List<Project>();
        foreach (var s in text)
        {
            var project = await QtRepository.GetProjectOrDefaultAsync(s, cancellationToken).ConfigureAwait(false);
            if (project is null)
                continue;
            projects.Add(project);
        }

        return projects;
    }


    protected bool MatchesByDistance(IEnumerable<string> source, IReadOnlyCollection<string> compare)
    {
        foreach (var s in source)
        {
            foreach (var q in compare)
            {
                if (s.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                    return true;
                // var distance = Levenshtein.Distance(s, q);
                // if (distance <= 2)
                //     return true;
            }
        }

        return false;
    }
}