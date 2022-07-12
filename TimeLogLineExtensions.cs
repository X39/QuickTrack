using JetBrains.Annotations;

namespace QuickTrack;

[PublicAPI]
public static class TimeLogLineExtensions
{
    /// <summary>
    /// Inserts the given <paramref name="insertLogLine"/> <see cref="TimeLogLine"/>
    /// after the provided <paramref name="afterLogLine"/> 
    /// into the input <paramref name="source"/>,
    /// shifting the <see cref="TimeLogLine"/>'s
    /// following <paramref name="afterLogLine"/> accordingly.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="afterLogLine"></param>
    /// <param name="insertLogLine"></param>
    /// <returns></returns>
    public static IEnumerable<TimeLogLine> InsertWithTimeShift(
        [NoEnumeration] this IEnumerable<TimeLogLine> source,
        TimeLogLine afterLogLine,
        TimeLogLine insertLogLine)
    {
        var distance = insertLogLine.TimeStampEnd - insertLogLine.TimeStampStart;
        var wasHit = false;
        foreach (var line in source)
        {
            if (wasHit)
            {
                yield return line with
                {
                    TimeStampStart = line.TimeStampStart + distance,
                    TimeStampEnd = line.TimeStampEnd == default ? default : line.TimeStampEnd + distance,
                };
            }
            else if (line == afterLogLine)
            {
                wasHit = true;
                yield return afterLogLine;
                yield return insertLogLine;
            }
            else
            {
                yield return line;
            }
        }
    }
}