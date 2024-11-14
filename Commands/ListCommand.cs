using System.Collections.Immutable;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;

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
    public string Pattern =>
        "list [ NUMBEROFDAYS | week | month ] [ ( of | from ) PROJECT ] [ ( in | at ) LOCATION ] [ total ( true | false | t | f | 1 | 0 ) ]";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var (daysToPrint, project, location, displayTotal) = await ParseArgumentsAsync(args, cancellationToken);

        async Task PrintTimeLog(
            Day day,
            TimeLog current,
            TimeLog? next,
            ConsoleStringFormatter consoleStringFormatter1,
            CancellationToken cancellationToken1)
        {
            if (project is not null && current.ProjectFk != project.Id)
                return;
            if (location is not null && current.LocationFk != location.Id)
                return;
            var consoleString = await current.ToConsoleString(
                    day,
                    consoleStringFormatter1,
                    cancellationToken1,
                    next)
                .ConfigureAwait(false);
            consoleString.WriteLine();
        }

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
            TimeLog? previous = null;
            var timeSpan = TimeSpan.Zero;
            await foreach (var log in day.GetTimeLogs(cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (previous is not null)
                {
                    await PrintTimeLog(day, previous, log, consoleStringFormatter, cancellationToken);
                    if (previous.Mode.IsCounted())
                        timeSpan += log.TimeStamp - previous.TimeStamp;
                }

                previous = log;
            }

            if (previous is not null)
                await PrintTimeLog(day, previous, null, consoleStringFormatter, cancellationToken);

            if (displayTotal)
            {
                new ConsoleString($"Total: {timeSpan}")
                {
                    Foreground = ConsoleColor.DarkYellow,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            }
        }
    }

    private async Task<(int daysToPrint, Project? project, Location? location, bool displayTotal)> ParseArgumentsAsync(
        ImmutableArray<string> args,
        CancellationToken cancellationToken)
    {
        var first = args.FirstOrDefault()?.ToLower().Trim();
        bool firstConsumed = false;
        int daysToPrint;
        switch (first)
        {
            case "*":
                daysToPrint   = int.MaxValue;
                firstConsumed = true;
                break;
            case "week":
                daysToPrint   = 7;
                firstConsumed = true;
                break;
            case "month":
                daysToPrint   = 31;
                firstConsumed = true;
                break;
            case { } when first.All(char.IsDigit):
                daysToPrint   = int.Parse(first);
                firstConsumed = true;
                break;
            default:
                daysToPrint = 1;
                break;
        }

        Project? project = null;
        Location? location = null;
        var displayTotal = true;
        for (var i = firstConsumed ? 1 : 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "from" when args.Length > i + 1:
                case "of" when args.Length > i + 1:
                    i++;
                    project = await args[i].GetProjectOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (project is null)
                    {
                        new ConsoleString($"No project named '{args[i]}' found")
                        {
                            Foreground = ConsoleColor.DarkYellow,
                            Background = ConsoleColor.Black,
                        }.WriteLine();
                    }

                    break;
                case "at" when args.Length > i + 1:
                case "in" when args.Length > i + 1:
                    i++;
                    location = await args[i].GetLocationOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (location is null)
                    {
                        new ConsoleString($"No location named '{args[i]}' found")
                        {
                            Foreground = ConsoleColor.DarkYellow,
                            Background = ConsoleColor.Black,
                        }.WriteLine();
                    }

                    break;
                case "total" when args.Length > i + 1:
                    i++;
                    displayTotal = args[i].ToLower() switch
                    {
                        "t"     => true,
                        "f"     => false,
                        "true"  => true,
                        "false" => false,
                        "1"     => true,
                        "0"     => false,
                        _       => false,
                    };
                    break;
                default:
                    new ConsoleString($"Unknown or unexpected argument '{args[i]}'")
                    {
                        Foreground = ConsoleColor.DarkYellow,
                        Background = ConsoleColor.Black,
                    }.WriteLine();
                    break;
            }
        }

        return (daysToPrint, project, location, displayTotal);
    }
}