using System.Collections.Immutable;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;

namespace QuickTrack.Commands;

public class ListCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"list"};

    public string Description =>
        "Lists the logged entries. " +
        "If no argument is provided, only today and yesterday will be listed. " +
        "If week is provided, the previous 7 days will be listed. " +
        "If month is provided, the previous 31 days will be listed. " +
        "If a number is provided, the previous X days will be listed where X is the number and 1 is today.";

    // ReSharper disable once StringLiteralTypo
    public string Pattern => "list [ NUMBEROFDAYS | week | month ]";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var first = args.FirstOrDefault()?.ToLower().Trim();
        var daysToPrint = first switch
        {
            "week"                           => 7,
            "month"                          => 31,
            { } when first.All(char.IsDigit) => int.Parse(first),
            _                                => 1,
        };

        await using var consoleStringFormatter = new ConsoleStringFormatter();
        var days = new List<Day>();
        await foreach (var day in QtRepository.GetDays(cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            if (daysToPrint-- <= 0)
                break;
            days.Add(day);
        }

        foreach (var day in days.OrderBy((q) => q.Date.ToDateOnly()))
        {
            await foreach (var log in day.GetTimeLogs(cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                var consoleString = await log.ToConsoleString(day, consoleStringFormatter, cancellationToken)
                    .ConfigureAwait(false);
                consoleString.WriteLine();
            }
        }
    }
}