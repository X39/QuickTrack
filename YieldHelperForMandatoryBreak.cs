#pragma warning disable CA2211
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util.Console;
using X39.Util;
using X39.Util.Collections;

namespace QuickTrack;

[PublicAPI]
public class YieldHelperForMandatoryBreak
{
    private record struct InsertPosition(int TotalLines, int LineIndex);

    private ConsoleStringFormatter _formatter;
    public ConsoleColor ListingBackgroundColor { get; set; } = ConsoleColor.DarkCyan;
    public ConsoleColor ValidRangeForegroundColor { get; set; } = ConsoleColor.White;
    public ConsoleColor InvalidRangeForegroundColor { get; set; } = ConsoleColor.Black;

    [Conditional("DEBUG")]
    private static void DebugLog(string message, int tab = 0)
    {
        new ConsoleString(tab > 0 ? string.Concat(new string(' ', tab * 4), message) : message)
        {
            Foreground = ConsoleColor.DarkGray,
            Background = ConsoleColor.Black,
        }.WriteLine();
    }

    public YieldHelperForMandatoryBreak(ConsoleStringFormatter consoleStringFormatter)
    {
        _formatter = consoleStringFormatter;
    }

    /// <summary>
    /// Returns the total pause in the given <paramref name="interval"/>
    /// </summary>
    /// <param name="timeLogs">All time logs of a <see cref="Day"/></param>
    /// <param name="interval">
    /// If 0, the first 6 hours + 1 are taken and checked for a pause of 30 minutes
    /// If 1, the 3 hours + 1 after the first 6 are taken and checked for a pause of 60 minutes
    /// If 2, the 3 hours + 1 after the first 9 are taken and checked for a pause of 60 minutes
    /// If N, the 3 hours + 1 after the first 6 + 3 * N and checked for a pause of 60 minutes
    /// </param>
    /// <returns>
    /// The range of the <see cref="TimeLog"/>'s in the given <paramref name="interval"/>.
    /// </returns>
    private static IEnumerable<TimeLog> GetWorkTimeInterval(IEnumerable<TimeLog> timeLogs, int interval)
    {
        var timeLogsArray = timeLogs as TimeLog[] ?? timeLogs.ToArray();
        if (timeLogsArray.None())
            return Enumerable.Empty<TimeLog>();
        var start = timeLogsArray.First().TimeStamp + interval switch
        {
            0 => TimeSpan.FromHours(0),
            _ => TimeSpan.FromHours(3 * (interval + 1)),
        };
        var end = start + interval switch
        {
            0 => TimeSpan.FromHours(6),
            _ => TimeSpan.FromHours(3),
        };
        return timeLogsArray.Where((q) => q.TimeStamp >= start && q.TimeStamp <= end);
    }

    private static TimeSpan SpanOfInterval(int interval)
    {
        return TimeSpan.FromHours(3 * (interval + 2));
    }

    private static TimeSpan PauseForInterval(int intervalValue)
    {
        return intervalValue == 0
            ? TimeSpan.FromMinutes(30)
            : TimeSpan.FromMinutes(45);
    }

    private static IEnumerable<(TimeLog timeLog, TimeSpan? timeSpan)> GetTimeSpans(IEnumerable<TimeLog> timeLogs)
    {
        TimeLog? previous = null;
        foreach (var timeLog in timeLogs)
        {
            if (previous is not null)
                yield return (previous, timeLog.TimeStamp - previous.TimeStamp);
            previous = timeLog;
        }

        if (previous is not null)
            yield return (previous, null);
    }

    private static TimeSpan GetTotalPause(IEnumerable<(TimeLog timeLog, TimeSpan? timeSpan)> tuples)
    {
        return tuples
            .Where((q) => !q.timeLog.Mode.IsCounted())
            .Select((q) => q.timeSpan)
            .NotNull()
            .DefaultIfEmpty(TimeSpan.Zero)
            .Aggregate((l, r) => l + r);
    }

    /// <summary>
    /// Inserts a <see cref="TimeLogLine"/>
    /// after the provided <paramref name="afterLogLine"/> 
    /// into the input <paramref name="source"/>,
    /// shifting the <see cref="TimeLogLine"/>'s
    /// following <paramref name="afterLogLine"/> accordingly.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="afterLogLine"></param>
    /// <param name="insertTimeSpan">The amount of time to shift the following entries by</param>
    /// <returns></returns>
    private static IEnumerable<TimeLog> InsertWithTimeShift(
        [NoEnumeration] IEnumerable<TimeLog> source,
        TimeLog afterLogLine,
        TimeSpan insertTimeSpan)
    {
        TimeLog? insertedLine = null;
        var wasHit = false;
        foreach (var line in source)
        {
            if (wasHit)
            {
                var copy = line.ShallowCopy();
                if (insertedLine is not null)
                {
                    insertedLine.TimeStamp = copy.TimeStamp;
                    yield return insertedLine;
                    insertedLine = null;
                }

                copy.TimeStamp = line.TimeStamp + insertTimeSpan;
                yield return copy;
            }
            else if (line == afterLogLine)
            {
                wasHit = true;
                yield return afterLogLine;

                insertedLine         = line.ShallowCopy();
                insertedLine.Mode    = ETimeLogMode.Break;
                insertedLine.Message = string.Empty;
            }
            else
            {
                yield return line;
            }
        }
    }

    public async IAsyncEnumerable<TimeLog> GetLinesWithMandatoryBreak(
        Day day,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        DebugLog($"Checking if {day.Date} is missing mandatory breaks");
        var intervalValue = -1;
        var timeLogs = await day.GetTimeLogsAsync(cancellationToken);
        var offset = TimeSpan.Zero;

        // Make sure we exceeded 6 hours
        if (GetTimeSpans(timeLogs)
                .Select((q) => q.timeSpan)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Aggregate((l, r) => l + r) <= TimeSpan.FromHours(6))
        {
            DebugLog("No adjustment for times needed as total < 6h");
            foreach (var timeLog in timeLogs)
            {
                yield return timeLog;
            }

            yield break;
        }

        //while (timeLogs.Any())
        {
            intervalValue++;
            // var intervalTimeLogs = GetWorkTimeInterval(timeLogs, intervalValue);
            var intervalTimeLogsWithSpans = GetTimeSpans(timeLogs.Select((q) => ShiftBy(q, offset)))
                .ToArray();

            DebugLog(
                // ReSharper disable once UseStringInterpolation
                string.Format(
                    "Handling interval counting {0} with {1}",
                    intervalTimeLogsWithSpans.Length,
                    string.Join(
                        ", ",
                        intervalTimeLogsWithSpans.Select(
                            (q) => string.Concat(
                                q.timeLog.TimeStamp.ToTimeOnly().ToString(),
                                " ",
                                q.timeSpan.ToString())))));
            if (intervalTimeLogsWithSpans.None())
                yield break;
            var totalPause = GetTotalPause(intervalTimeLogsWithSpans);
            var mandatoryPause = PauseForInterval(intervalValue);
            if (totalPause >= mandatoryPause)
            {
                DebugLog($"Expected pause of {mandatoryPause}, got {totalPause}");
                foreach (var (timeLog, _) in intervalTimeLogsWithSpans)
                {
                    yield return timeLog;
                }
            }
            else
            {
                var remainingMandatoryPause = mandatoryPause - totalPause;
                DebugLog(
                    $"Expected pause of {mandatoryPause}, got {totalPause} with remaining {remainingMandatoryPause}");
                var line = await GetTimeLogLineForPauseAsync(
                    day,
                    intervalTimeLogsWithSpans,
                    totalPause,
                    remainingMandatoryPause,
                    intervalValue,
                    cancellationToken);
                foreach (var timeLog in InsertWithTimeShift(
                             intervalTimeLogsWithSpans.Select((q) => q.timeLog),
                             line,
                             remainingMandatoryPause))
                {
                    yield return timeLog;
                }

                offset += remainingMandatoryPause;
            }
        }

        //throw new NotImplementedException();
        //var lines = timeLogLinesFile.GetLines().ToArray();
        //var totalPause = GetTotalPause(lines);
        //if (totalPause >= MandatoryPause)
        //    return lines;
        //var remainingMandatoryPause = MandatoryPause - totalPause;
        //var line = GetTimeLogLineForPause(timeLogLinesFile, lines, totalPause, remainingMandatoryPause);
        //return lines.InsertWithTimeShift(
        //        line,
        //        new TimeLog(
        //            line.TimeStampEnd,
        //            line.TimeStampEnd + remainingMandatoryPause,
        //            string.Empty,
        //            Constants.ProjectForBreak,
        //            Constants.MessageForBreak))
        //    .ToArray();
    }

    private TimeLog ShiftBy(TimeLog timeLog, TimeSpan offset)
    {
        var copy = timeLog.ShallowCopy();
        copy.TimeStamp += offset;
        return copy;
    }

    private async Task<TimeLog> GetTimeLogLineForPauseAsync(
        Day day,
        (TimeLog timeLog, TimeSpan? timeSpan)[] lines,
        TimeSpan totalPause,
        TimeSpan remainingMandatoryPause,
        int interval,
        CancellationToken cancellationToken)
    {
        var jsonAttachment = await day
            .GetJsonAttachmentAsync(typeof(YieldHelperForMandatoryBreak).FullName(), cancellationToken)
            .ConfigureAwait(false);

        async ValueTask<TimeLog> Func(JsonPayload jsonPayload, CancellationToken token)
        {
            if (jsonPayload.Insertions.TryGetValue(interval, out var intervalData)
                && intervalData.IntervalCount == lines.Length
                && intervalData.AfterTimeLogIndex < lines.Length
                && intervalData.AfterTimeLogIndex >= 0)
            {
                return lines[intervalData.AfterTimeLogIndex].timeLog;
            }

            // ReSharper disable once ConditionalTernaryEqualBranch
            DebugLog(
                intervalData is null
                    ? "No interval data found"
                    : $"Interval data has count of {intervalData.IntervalCount} but {lines.Length} have been passed in.");

            Console.WriteLine($"Missing mandatory break time on {day.Date.ToDateOnly().ToLongDateString()}.");
            Console.WriteLine($"Got {totalPause} of break time, expected at least {PauseForInterval(interval)}.");
            Console.WriteLine($"Please choose where to append {remainingMandatoryPause} of mandatory break time.");
            Console.WriteLine($"Time will be appended AFTER the selected entry.");
            var linesTuples = new List<(TimeLog timeLog, DateTime? end, string text)>();
            foreach (var timeLogWithSpan in lines)
            {
                var text = await timeLogWithSpan.timeLog
                    .ToConsoleString(day, _formatter, token)
                    .ConfigureAwait(false);
                var end = timeLogWithSpan.timeLog.TimeStamp + timeLogWithSpan.timeSpan;
                linesTuples.Add((timeLogWithSpan.timeLog, end, text));
            }

            var max = linesTuples.First().timeLog.TimeStamp + SpanOfInterval(interval);
            var (selectedTimeLog, _, _) = AskConsole.ForValueFromCollection(
                linesTuples,
                (q) => new ConsoleString
                {
                    Text = q.text,
                    Foreground = q.end > max || q.end is null
                        ? InvalidRangeForegroundColor
                        : ValidRangeForegroundColor,
                    Background = ListingBackgroundColor,
                },
                new ConsoleString("Selection is not valid.")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                },
                token);
            var lineIndex = lines.Indexed().First((q) => q.value.timeLog == selectedTimeLog).index;
            jsonPayload.Insertions[interval] = new JsonPayload.IntervalData
            {
                IntervalCount     = lines.Length,
                AfterTimeLogIndex = lineIndex,
            };
            DebugLog($"User selected {lineIndex} with {lines.Length} lines count");
            return selectedTimeLog;
        }

        var result = await jsonAttachment.WithDoAsync<JsonPayload, TimeLog>(Func, cancellationToken)
            .ConfigureAwait(false);
        await jsonAttachment.UpdateAsync(cancellationToken);
        return result;
    }

    public class JsonPayload
    {
        public class IntervalData
        {
            public int AfterTimeLogIndex { get; init; }
            public int IntervalCount { get; init; }
        }

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Dictionary<int, IntervalData> Insertions { get; set; } = new();
    }
}