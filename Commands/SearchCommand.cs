using System.Collections.Immutable;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class SearchCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"search"};

    public string Description =>
        "Searches the given number of log-files for the given TEXT. " +
        "If no argument is provided, only today and yesterday will be listed. " +
        "If * is provided, all log-files will be searched. " +
        "If week is provided, the previous 7 days will be listed. " +
        "If month is provided, the previous 31 days will be listed. " +
        "If a number is provided, the previous X days will be listed where X is the number and 1 is today.";

    // ReSharper disable once StringLiteralTypo
    public string Pattern => "search ( NUMBEROFDAYS | week | month ) TEXT...";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        if (args.Length < 2)
        {
            new ConsoleString("Please provide a number of days and a text to search for.")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        var first = args.First().ToLower().Trim();
        var days = first switch
        {
            "week"                           => 7,
            "month"                          => 31,
            "*"                              => int.MaxValue,
            { } when first.All(char.IsDigit) => int.Parse(first),
            _                                => 1,
        } - 1;

        var separators = new[] {' ', '[', ']', '|', ':'};
        var text = args
            .Skip(1)
            .SelectMany((arg) => arg.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            .ToImmutableArray();
        await using var consoleStringFormatter = new ConsoleStringFormatter();
        await foreach (var day in QtRepository.GetDays(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (days-- <= 0)
                return;
            await foreach (var timeLog in day.GetTimeLogs(cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                if (!MatchesByDistance(timeLog.Message.Split(separators), text))
                    continue;
                await timeLog.ToConsoleString(day, consoleStringFormatter, cancellationToken);
            }
        }
    }

    private bool MatchesByDistance(IEnumerable<string> source, IReadOnlyCollection<string> compare)
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