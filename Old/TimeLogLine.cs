using System.Diagnostics;
using System.Text;
using TextCopy;
using X39.Util;

namespace QuickTrack;
[Obsolete]
public record TimeLogLine(
    DateTime TimeStampStart,
    DateTime TimeStampEnd,
    string Location,
    string Project,
    string Message)
{
    public override string ToString()
    {
        return string.Concat(
            "[",
            TimeStampStart.ToLocalTime().ToTimeOnly().ToString("HH:mm"),
            " - ",
            TimeStampEnd == default
                ? "--:--"
                : TimeStampEnd.ToLocalTime().ToTimeOnly().ToString("HH:mm"),
            "] ",
            Project,
            ": ",
            Message);
    }

    public TimeSpan Duration => TimeStampEnd == default
        ? TimeSpan.Zero
        : TimeStampEnd - TimeStampStart;

    public DateTime TimeStampStartRounded => new(
        TimeStampStart.Year,
        TimeStampStart.Month,
        TimeStampStart.Day,
        TimeStampStart.Hour,
        TimeStampStart.Minute,
        0);

    public DateTime TimeStampEndRounded => new(
        TimeStampEnd.Year,
        TimeStampEnd.Month,
        TimeStampEnd.Day,
        TimeStampEnd.Hour,
        TimeStampEnd.Minute,
        0);

    public TimeSpan DurationRounded =>
        TimeStampEnd == default
            ? TimeSpan.Zero
            : TimeStampEndRounded - TimeStampStartRounded;

    public bool IsPause => Project is Constants.ProjectForBreak;
}