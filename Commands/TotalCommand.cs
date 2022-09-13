using System.Collections.Immutable;
using System.Diagnostics;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class TotalCommand : IConsoleCommand
{
    public string[] Keys { get; } = new[] {"total"};

    public string Description =>
        "Displays the total time spent of the current month and displays a delta, showing " +
        "how much time was spent above/below 8 hours per day.";

    public string Pattern => "total";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var delta = TimeSpan.Zero;
        var referenceTime = TimeSpan.Zero;
        await foreach (var month in QtRepository.GetDaysByMonth(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            var days = month.Days;
            var (newReferenceTime, _, newDelta) = await GetTimeTotalOfRange(days, cancellationToken);
            PrintInfo(newReferenceTime, newDelta, (month.Year, month.Month));
            delta         += newDelta;
            referenceTime += newReferenceTime;
        }

        PrintInfo(referenceTime, delta);
    }

    private async Task<(TimeSpan referenceTime, TimeSpan actualTime, TimeSpan delta)> GetTimeTotalOfRange(
        IReadOnlyCollection<Day> days,
        CancellationToken cancellationToken)
    {
        const int hoursPerDay = 8;
        var entries = days.Count;
        var referenceTime = TimeSpan.FromHours(hoursPerDay * entries);
        var actualTime = TimeSpan.Zero;
        var today = DateTime.Today.ToDateOnly();
        foreach (var (day, index) in days.Indexed())
        {
            var lines = await day.GetTimeLogsAsync(cancellationToken: cancellationToken);
            if (day.Date == today)
            {
                lines = lines.Append(
                    new TimeLog
                    {
                        Day = day,
                        TimeStamp = DateTime.Now,
                    })
                    .ToImmutableArray();
            }

            DebugWriteLine(day.Date.ToString(), ConsoleColor.Cyan);
            var result = TimeSpan.Zero;
            var lastTimeStamp = lines.First().TimeStamp.RoundToMinutes();
            foreach (var q in lines)
            {
                if (q.Mode == ETimeLogMode.Break)
                    continue;
                DebugWriteLine(q.ToString());
                DebugWriteLine(
                    $"referenceTime: {TimeSpan.FromHours(hoursPerDay * (index + 1))}, " +
                    $"actualTime: {actualTime + result}, " +
                    $"result: {result}, " +
                    $"delta: {TimeSpan.FromHours(hoursPerDay * (index + 1)) - (actualTime + result)}");

                var tmp = q.TimeStamp.RoundToMinutes();
                result        += tmp - lastTimeStamp;
                lastTimeStamp =  tmp;
            }

            actualTime += result;
        }

        var delta = referenceTime - actualTime;
        return (referenceTime, actualTime, delta);
    }

    [Conditional("DEBUG")]
    private static void DebugWriteLine(string s, ConsoleColor color = ConsoleColor.Gray)
    {
        new ConsoleString(s)
        {
            Foreground = color
        }.WriteLine();
    }

    private static void PrintInfo(TimeSpan referenceTime, TimeSpan delta, (int Year, int Month)? tuple = default)
    {
        if (tuple is not null)
        {
            new ConsoleString($"[{tuple.Value.Month:00}.{tuple.Value.Year:0000}] With the mandatory time of ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();
        }
        else
        {
            new ConsoleString("Of the mandatory time ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();
        }

        new ConsoleString(referenceTime.ToString("dd\\.hh\\:mm\\:ss"))
        {
            Foreground = ConsoleColor.Yellow,
            Background = ConsoleColor.Black,
        }.Write();
        new ConsoleString(" you have ")
        {
            Foreground = ConsoleColor.White,
            Background = ConsoleColor.Black,
        }.Write();
        if (delta < TimeSpan.Zero)
        {
            new ConsoleString(delta.ToString("dd\\.hh\\:mm\\:ss"))
            {
                Foreground = ConsoleColor.Green,
                Background = ConsoleColor.Black,
            }.Write();
            if (tuple is null)
            {
                new ConsoleString(" additionally spent if ending your day now.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            }
            else
            {
                new ConsoleString(" gained.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            }
        }
        else
        {
            new ConsoleString(delta.ToString("dd\\.hh\\:mm\\:ss"))
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.Write();
            if (tuple is null)
            {
                new ConsoleString($" remaining (")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                new ConsoleString($"{(DateTime.Now + delta).ToTimeOnly()}")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.Write();
                new ConsoleString($") if ending your day now.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            }
            else
            {
                new ConsoleString(" remaining.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.WriteLine();
            }
        }
    }
}