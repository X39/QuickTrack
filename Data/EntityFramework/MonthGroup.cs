using System.Collections.Immutable;
using QuickTrack.Data.Database;

namespace QuickTrack.Data.EntityFramework;

public record MonthGroup(byte Month, short Year, ImmutableArray<Day> Days);