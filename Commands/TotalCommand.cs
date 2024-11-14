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
    const string TimeSpanFormat = @"dd\.hh\:mm";
    public string[] Keys { get; } = new[] {"total"};

    public string Description =>
        "Displays the total time spent of the current month and displays a delta, showing " +
        "how much time was spent above/below 8 hours per day.";

    public string Pattern => "total";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var fullDelta = TimeSpan.Zero;
        var referenceTime = TimeSpan.Zero;
        var months = new List<MonthGroup>();
        await foreach (var month in QtRepository.GetDaysByMonth(cancellationToken))
        {
            months.Add(month);
        }

        foreach (var (month, year, days) in months.OrderBy((q) => q.Year).ThenBy((q) => q.Month))
        {
            var (newReferenceTime, _, newDelta) =  await GetTimeTotalOfRange(days, cancellationToken);
            fullDelta                           += newDelta;
            PrintInfo(newReferenceTime, newDelta, fullDelta, (Year: year, Month: month));
            referenceTime += newReferenceTime;
        }

        PrintInfo(referenceTime, fullDelta, fullDelta, null);
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
        foreach (var (day, index) in days.OrderBy((q) => q.Date.ToDateOnly()).Indexed())
        {
            var lines = await day.GetTimeLogsAsync(cancellationToken: cancellationToken);
            if (day.Date == today)
            {
                lines = lines.Append(
                        new TimeLog
                        {
                            Day       = day,
                            TimeStamp = DateTime.Now,
                        })
                    .ToImmutableArray();
            }

            DebugWriteLine(day.Date.ToString(), ConsoleColor.Cyan);
            if (lines.None())
            {
                actualTime += TimeSpan.FromHours(hoursPerDay);
                continue;
            }

            var result = TimeSpan.Zero;
            var lastTimeStamp = lines.First().TimeStamp.RoundToMinutes();
            var wasPause = false;
            foreach (var q in lines)
            {
                var tmp = q.TimeStamp.RoundToMinutes();
                if (wasPause)
                {
                    DebugWriteLine(
                        $"referenceTime: {TimeSpan.FromHours(hoursPerDay * (index + 1))}, " +
                        $"actualTime: {actualTime + result}, " +
                        $"result: {result}, " +
                        $"delta: {TimeSpan.FromHours(hoursPerDay * (index + 1)) - (actualTime + result)}, " +
                        $"local-delta: {tmp - lastTimeStamp}");
                    DebugWriteLine(q.ToString());
                    lastTimeStamp = tmp;
                    wasPause      = false;
                    continue;
                }

                if (!q.Mode.IsCounted())
                    wasPause = true;

                result += tmp - lastTimeStamp;
                DebugWriteLine(
                    $"referenceTime: {TimeSpan.FromHours(hoursPerDay * (index + 1))}, " +
                    $"actualTime: {actualTime + result}, " +
                    $"result: {result}, " +
                    $"delta: {TimeSpan.FromHours(hoursPerDay * (index + 1)) - (actualTime + result)}, " +
                    $"local-delta: {tmp - lastTimeStamp}");
                lastTimeStamp = tmp;
                DebugWriteLine(q.ToString());
            }

            DebugWriteLine(
                $"referenceTime: {TimeSpan.FromHours(hoursPerDay * (index + 1))}, " +
                $"actualTime: {actualTime + result}, " +
                $"result: {result}, " +
                $"delta: {TimeSpan.FromHours(hoursPerDay * (index + 1)) - (actualTime + result)}");
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

    private static void PrintInfo(
        TimeSpan referenceTime,
        TimeSpan individualDelta,
        TimeSpan fullDelta,
        (int Year, int Month)? tuple = default)
    {
        static void WriteTime(TimeSpan timeSpan, string prefix = "", string suffix = "", bool includeSign = false)
        {
            var color = timeSpan > TimeSpan.Zero
                ? ConsoleColor.Red
                : timeSpan < TimeSpan.Zero
                    ? ConsoleColor.Green
                    : ConsoleColor.Yellow;
            if (prefix.IsNotNullOrWhiteSpace())
            {
                new ConsoleString(prefix)
                {
                    Foreground = color,
                    Background = ConsoleColor.Black,
                }.Write();
            }

            if (includeSign)
            {
                new ConsoleString(timeSpan > TimeSpan.Zero ? "-" : "+")
                {
                    Foreground = color,
                    Background = ConsoleColor.Black,
                }.Write();
            }

            new ConsoleString(timeSpan.ToString(TimeSpanFormat))
            {
                Foreground = color,
                Background = ConsoleColor.Black,
            }.Write();
            if (suffix.IsNotNullOrWhiteSpace())
            {
                new ConsoleString(suffix)
                {
                    Foreground = color,
                    Background = ConsoleColor.Black,
                }.Write();
            }
        }

        individualDelta = individualDelta.RoundToMinutes();
        fullDelta       = fullDelta.RoundToMinutes();
        if (tuple is not null)
        {
            new ConsoleString($"[{tuple.Value.Month:00}.{tuple.Value.Year:0000}] ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();

            new ConsoleString("Of the mandatory time ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();
            new ConsoleString(referenceTime.ToString(TimeSpanFormat))
            {
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            new ConsoleString(" you have a time delta of ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();
            WriteTime(fullDelta, includeSign: true);
            new ConsoleString(". ")
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
            new ConsoleString(referenceTime.ToString(TimeSpanFormat))
            {
                Foreground = ConsoleColor.Yellow,
                Background = ConsoleColor.Black,
            }.Write();
            new ConsoleString(" you have a time delta of ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();
            WriteTime(fullDelta, includeSign: true);
            new ConsoleString(" as of now. ")
            {
                Foreground = ConsoleColor.White,
                Background = ConsoleColor.Black,
            }.Write();
        }

        switch (tuple)
        {
            case null when individualDelta < TimeSpan.Zero:
                break;
            case null when individualDelta > TimeSpan.Zero:
                new ConsoleString($"The delta will amount to zero at ")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                new ConsoleString($"{(DateTime.Now + individualDelta).ToTimeOnly()}")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.Write();
                new ConsoleString($".")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                break;
            case null:
                new ConsoleString($" No change to time balance is done if ending your day now.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                break;
            case not null when individualDelta < TimeSpan.Zero:
                WriteTime(individualDelta);
                new ConsoleString(" added to time delta.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                break;
            case not null when individualDelta > TimeSpan.Zero:
                WriteTime(individualDelta);
                new ConsoleString(" removed from time delta.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                break;
            case not null:
                WriteTime(individualDelta);
                new ConsoleString(" or in words nothing was changed.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Black,
                }.Write();
                break;
        }

        Console.WriteLine();
    }
}