using System.Diagnostics;
using System.Text;
using TextCopy;

namespace QuickTrack;

public record TimeLogLine(DateTime TimeStampStart, DateTime TimeStampEnd, string Project, string Message);