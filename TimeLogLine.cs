using System.Diagnostics;
using System.Text;
using TextCopy;
using X39.Util;

namespace QuickTrack;

public record TimeLogLine(DateTime TimeStampStart, DateTime TimeStampEnd, string Project, string Message)
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
}