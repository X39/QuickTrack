using QuickTrack.Data.Database;

namespace QuickTrack;

public static class TimeLogModeExtensions
{
    public static bool IsNotExported(this ETimeLogMode self) => self switch
    {
        ETimeLogMode.Normal  => false,
        ETimeLogMode.Break   => true,
        ETimeLogMode.Quit    => true,
        ETimeLogMode.Export  => false,
        ETimeLogMode.OffTime => true,
        _                    => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };
    public static bool IsCounted(this ETimeLogMode self) => self switch
    {
        ETimeLogMode.Normal  => true,
        ETimeLogMode.Break   => false,
        ETimeLogMode.Quit    => true,
        ETimeLogMode.Export  => true,
        ETimeLogMode.OffTime => true,
        _                    => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };
}