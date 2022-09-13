using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using X39.Util;

namespace QuickTrack.Data.Database;

[Owned]
[Index(nameof(Day), nameof(Month), nameof(Year), IsUnique = true)]
[Index(nameof(Month), nameof(Year))]
[Index(nameof(Year))]
public class DateComplex
{
    protected bool Equals(DateComplex other)
    {
        return Day == other.Day && Month == other.Month && Year == other.Year;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DateComplex) obj);
    }

    /// <summary>
    /// Day of month
    /// </summary>
    /// <remarks>
    /// 1st is 1,
    /// 31st is 31.
    /// </remarks>
    [ValueRange(1, 31)]
    public byte Day { get; set; }
    
    /// <summary>
    /// Month of year
    /// </summary>
    /// <remarks>
    /// January is 1,
    /// December is 12.
    /// </remarks>
    [ValueRange(1, 12)]
    public byte Month { get; set; }
    public short Year { get; set; }

    [NotMapped]
    public DateOnly Date
    {
        get => new DateOnly(Year, Month, Day);
        set
        {
            Year  = (short) value.Year;
            Month = (byte) value.Month;
            Day   = (byte) value.Day;
        }
    }

    public static DateComplex Today => DateTime.Today.ToDateOnly();

    public DateOnly ToDateOnly() => new(Year, Month, Day);
    public DateTime ToDateTime() => new(Year, Month, Day);
    public DateTime ToDateTime(TimeOnly timeOnly) => new(Year, Month, Day, timeOnly.Hour, timeOnly.Second, timeOnly.Millisecond);

    public static implicit operator DateComplex(DateOnly self) => new() {Date = self};
    public static implicit operator DateComplex(DateTime self) => new() {Date = self.ToDateOnly()};
    public static implicit operator DateOnly(DateComplex self) => self.ToDateOnly();
    public static implicit operator DateTime(DateComplex self) => new(self.Year, self.Month, self.Day);
    public override string ToString() => $"{Year:0000}-{Month:00}-{Day:00}";

}