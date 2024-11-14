using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using QuickTrack.Data.Database;
using QuickTrack.Data.Meta;
using X39.Util;

namespace QuickTrack.Data.EntityFramework;

public static class QtRepository
{
    private static QtContext CreateContext()
    {
        return new QtContext();
    }

    public static async Task<Day> GetDayAsync(
        this DateOnly date,
        object? source,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        DateComplex complex = date;
        var query = from day in context.Days
            where day.Date.Year == complex.Year
            where day.Date.Month == complex.Month
            where day.Date.Day == complex.Day
            select day;
        var single = await query.SingleOrDefaultAsync(cancellationToken);
        if (single is not null)
            return single;
        var now = DateTime.Now;
        single = new Day
        {
            Date = complex,
            AuditLog = new List<Audit>
            {
                new()
                {
                    Source    = source?.GetType().FullName() ?? string.Empty,
                    Message   = "Day created",
                    TimeStamp = now,
                    Kind      = EAuditKind.DayCreated,
                }
            }
        };
        await context.Days.AddAsync(single, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return single;
    }

    public static async Task<Day?> GetDayOrDefaultAsync(
        this DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        DateComplex complex = date;
        var query = from day in context.Days
            where day.Date.Year == complex.Year
            where day.Date.Month == complex.Month
            where day.Date.Day == complex.Day
            select day;
        var single = await query.SingleOrDefaultAsync(cancellationToken);
        return single;
    }

    public static async Task<Location> GetLocationAsync(
        this string title,
        CancellationToken cancellationToken = default)
    {
        title = title.Trim();
        await using var context = CreateContext();
        var single = await context.Locations
            .SingleOrDefaultAsync((location) => location.Title == title, cancellationToken);
        if (single is not null)
            return single;
        var now = DateTime.Now;
        single = new Location
        {
            Title            = title,
            TimeStampCreated = now,
        };
        await context.Locations.AddAsync(single, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Project> GetProjectAsync(this string title, CancellationToken cancellationToken = default)
    {
        title = title.Trim();
        await using var context = CreateContext();
        var single = await context.Projects
            .SingleOrDefaultAsync((project) => project.Title == title, cancellationToken)
            .ConfigureAwait(false);
        if (single is not null)
            return single;
        var now = DateTime.Now;
        single = new Project
        {
            Title            = title,
            TimeStampCreated = now,
        };
        await context.Projects.AddAsync(single, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Project> GetProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var single = await context.Projects
            .SingleAsync((day) => day.Id == projectId, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static Task<Project> GetProjectAsync(
        this TimeLog self,
        CancellationToken cancellationToken = default)
    {
        return GetProjectAsync(self.ProjectFk, cancellationToken);
    }

    public static async Task<Project?> GetProjectOrDefaultAsync(
        this string projectName,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var single = await context.Projects
            .SingleOrDefaultAsync((day) => day.Title == projectName, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Location?> GetLocationOrDefaultAsync(
        this string locationName,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var single = await context.Locations
            .SingleOrDefaultAsync((day) => day.Title == locationName, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Project?> GetProjectOrDefaultAsync(
        int? projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectId is null)
            return null;
        await using var context = CreateContext();
        var single = await context.Projects
            .SingleOrDefaultAsync((day) => day.Id == projectId, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Day?> GetDayOrDefaultAsync(int dayId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var single = await context.Days
            .SingleOrDefaultAsync((day) => day.Id == dayId, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Location> GetLocationAsync(
        int locationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var single = await context.Locations
            .SingleAsync((day) => day.Id == locationId, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Location?> GetLocationOrDefaultAsync(
        int? locationId,
        CancellationToken cancellationToken = default)
    {
        if (locationId is null)
            return null;
        await using var context = CreateContext();
        var single = await context.Locations
            .SingleOrDefaultAsync((day) => day.Id == locationId, cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async IAsyncEnumerable<Location> GetLocations(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = context.Locations
            .OrderBy((q) => q.Title)
            .AsAsyncEnumerable();
        await foreach (var location in range.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return location;
        }
    }

    public static async Task<IReadOnlyCollection<Project>> GetProjectsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = await context.Projects
            .OrderBy((q) => q.Title)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return range.ToImmutableArray();
    }

    public static async Task<IReadOnlyCollection<Location>> GetLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = await context.Locations
            .OrderBy((q) => q.Title.ToLower())
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return range.ToImmutableArray();
    }

    public static async IAsyncEnumerable<Project> GetProjects(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = context.Projects
            .OrderBy((q) => q.Title.ToLower())
            .AsAsyncEnumerable();
        await foreach (var project in range
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return project;
        }
    }

    public static async IAsyncEnumerable<Day> GetDays(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = context.Days
            .OrderByDescending((q) => q.Date.Year)
            .ThenByDescending((q) => q.Date.Month)
            .ThenByDescending((q) => q.Date.Day)
            .AsAsyncEnumerable();
        await foreach (var timeLog in range.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return timeLog;
        }
    }

    public static async IAsyncEnumerable<MonthGroup> GetDaysByMonth(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = context.Days
            .OrderByDescending((q) => q.Date.Year)
            .ThenByDescending((q) => q.Date.Month)
            .ThenByDescending((q) => q.Date.Day)
            .AsAsyncEnumerable()
            .ConfigureAwait(false);
        var days = new List<Day>();
        await foreach (var day in range
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            var firstDay = days.FirstOrDefault();
            if (firstDay is not null && firstDay.Date.Month != day.Date.Month)
            {
                yield return new MonthGroup(
                    firstDay.Date.Month,
                    firstDay.Date.Year,
                    days.ToImmutableArray());
                days.Clear();
            }

            days.Add(day);
        }

        if (days.FirstOrDefault() is not { } lastFirstDay)
            yield break;
        yield return new MonthGroup(
            lastFirstDay.Date.Month,
            lastFirstDay.Date.Year,
            days.ToImmutableArray());
        days.Clear();
    }

    public static async IAsyncEnumerable<TimeLog> GetTimeLogs(
        this Day day,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = context.TimeLogs
            .Where((timeLog) => timeLog.DayFk == day.Id)
            .OrderBy((timeLog) => timeLog.TimeStamp)
            .AsAsyncEnumerable()
            .ConfigureAwait(false);
        await foreach (var timeLog in range.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return timeLog;
        }
    }

    public static async Task<bool> HasTimeLogsAsync(this Project self, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var projectId = self.Id;
        return context.TimeLogs.Any((q) => q.ProjectFk == projectId);
    }

    public static async Task DeleteAsync(this Project self, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        context.Projects.Attach(self);
        var projectId = self.Id;
        if (context.TimeLogs.Any((q) => q.ProjectFk == projectId))
            throw new InvalidOperationException($"Project still has TimeLogs.");
        context.Projects.Remove(self);
        await context.SaveChangesAsync(cancellationToken);
    }

    public static async IAsyncEnumerable<TimeLog> GetTimeLogs(
        this Project project,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var projectId = project.Id;
        var range = context.TimeLogs
            .Where((timeLog) => timeLog.ProjectFk == projectId)
            .OrderBy((timeLog) => timeLog.TimeStamp)
            .AsAsyncEnumerable()
            .ConfigureAwait(false);
        await foreach (var timeLog in range.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return timeLog;
        }
    }
    
    public static async IAsyncEnumerable<Day> GetDays(this Project project, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var projectId = project.Id;
        var range = context.TimeLogs
            .Where((timeLog) => timeLog.ProjectFk == projectId)
            .Select((timeLog) => timeLog.Day!)
            .Distinct()
            .OrderByDescending((day) => day.Date.Year)
            .ThenByDescending((day) => day.Date.Month)
            .ThenByDescending((day) => day.Date.Day)
            .AsAsyncEnumerable()
            .ConfigureAwait(false);
        await foreach (var day in range.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return day;
        }
    }

    public static async Task<TimeLog?> GetMostRecentNormalTimeLog(bool todayOnly, CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        if (todayOnly)
        {
            var today = DateTime.Today;
            var timeLog = await context.TimeLogs
                .Where((timeLog) => timeLog.Mode == ETimeLogMode.Normal)
                .Where((timeLog) => timeLog.TimeStamp >= today)
                .OrderByDescending((timeLog) => timeLog.TimeStamp)
                .Take(1)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return timeLog;
        }
        else
        {
            var timeLog = await context.TimeLogs
                .Where((timeLog) => timeLog.Mode == ETimeLogMode.Normal)
                .OrderByDescending((timeLog) => timeLog.TimeStamp)
                .Take(1)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return timeLog;
        }
    }

    public static async Task<IReadOnlyCollection<TimeLog>> GetTimeLogsAsync(
        this Day day,
        CancellationToken cancellationToken = default)
    {
        var timeLogs = day.GetTimeLogs(cancellationToken);
        var output = new List<TimeLog>();
        await foreach (var timeLog in timeLogs.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            output.Add(timeLog);
        }

        return output;
    }

    public static async IAsyncEnumerable<Audit> GetAudits(
        this Day day,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var range = context.Audits
            .Where((audit) => audit.DayFk == day.Id)
            .AsAsyncEnumerable();
        await foreach (var timeLog in range.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return timeLog;
        }
    }

    public static async Task<TimeLog> AppendTimeLogAsync(
        this Day day,
        object? source,
        Location location,
        Project project,
        ETimeLogMode mode,
        string message,
        DateTime? timeStamp = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateContext();
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        var now = DateTime.Now;
        timeStamp ??= now;
        timeStamp = new DateTime(
            timeStamp.Value.Year,
            timeStamp.Value.Month,
            timeStamp.Value.Day,
            timeStamp.Value.Hour,
            timeStamp.Value.Minute,
            timeStamp.Value.Second,
            timeStamp.Value.Millisecond);
        var timeLog = new TimeLog
        {
            DayFk      = day.Id,
            ProjectFk  = project.Id,
            LocationFk = location.Id,
            TimeStamp  = timeStamp.Value,
            Message    = string.Concat(message.Trim().TrimEnd('.'), '.'),
            Mode       = mode,
        };
        await dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        var audit = new Audit
        {
            DayFk     = day.Id,
            TimeStamp = now,
            Source    = source?.GetType().FullName() ?? string.Empty,
            Message = $"Added time log for {timeStamp} with location '{location.Title}' " +
                      $"({location.Id}), project '{project.Title}' ({project.Id}) and message '{message}'.",
            Json = JsonSerializer.Serialize(timeLog),
            Kind = EAuditKind.LogLineAppended,
        };
        await dbContext.TimeLogs.AddAsync(timeLog, cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Audits.AddAsync(audit, cancellationToken)
            .ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbTransaction.CommitAsync(cancellationToken)
            .ConfigureAwait(false);
        return timeLog;
    }

    public static async Task<Audit> AppendAuditAsync(
        this Day day,
        object? source,
        EAuditKind kind,
        string message,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateContext();
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        var now = DateTime.Now;
        now = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            now.Second,
            now.Millisecond);
        await dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        var audit = new Audit
        {
            DayFk     = day.Id,
            TimeStamp = now,
            Source    = source?.GetType().FullName() ?? string.Empty,
            Message   = message.Trim(),
            Kind      = EAuditKind.Note,
        };
        await dbContext.Audits.AddAsync(audit, cancellationToken)
            .ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbTransaction.CommitAsync(cancellationToken)
            .ConfigureAwait(false);
        return audit;
    }

    public static async Task UpdateAsync(
        this TimeLog timeLog,
        object? source,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var now = DateTime.Now;
        var existing = await context.TimeLogs.SingleOrDefaultAsync((q) => q.Id == timeLog.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new ArgumentException("The provided TimeLog could not be found in the database.");
        foreach (var propertyInfo in typeof(TimeLog).GetProperties(
                     BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Instance))
        {
            if (propertyInfo.Name switch
                {
                    nameof(TimeLog.Id)              => true,
                    nameof(TimeLog.DayFk)           => true,
                    nameof(TimeLog.Day)             => false,
                    nameof(TimeLog.Project)         => true,
                    nameof(TimeLog.ProjectFk)       => false,
                    nameof(TimeLog.Location)        => true,
                    nameof(TimeLog.LocationFk)      => false,
                    nameof(TimeLog.JsonAttachments) => true,
                    _                               => false,
                })
                continue;
            var value = propertyInfo.GetValue(timeLog);
            if (value is string s && propertyInfo.Name is nameof(TimeLog.Message))
                value = string.Concat(s.Trim().TrimEnd('.'), '.');
            var existingValue = propertyInfo.GetValue(existing);

            if (existingValue is null && value is null
                || existingValue is null && value is not null
                || existingValue is not null && value is not null && existingValue.Equals(value))
                continue;
            Audit audit;
            if (existingValue is DateTimeOffset existingDateTimeOffset && value is DateTimeOffset valueDateTimeOffset)
            {
                audit = new Audit
                {
                    DayFk     = existing.DayFk,
                    TimeStamp = now,
                    Source    = source?.GetType().FullName() ?? string.Empty,
                    Message = $"Changing {propertyInfo.Name} of {existing.Id} ({existing.TimeStamp}) " +
                              $"from '{existingDateTimeOffset:O}' to '{valueDateTimeOffset:O}'.",
                    Kind = EAuditKind.LogLineUpdated,
                };
            }
            else if (existingValue is DateTime existingDateTime && value is DateTime valueDateTime)
            {
                audit = new Audit
                {
                    DayFk     = existing.DayFk,
                    TimeStamp = now,
                    Source    = source?.GetType().FullName() ?? string.Empty,
                    Message = $"Changing {propertyInfo.Name} of {existing.Id} ({existing.TimeStamp}) " +
                              $"from '{existingDateTime:O}' to '{valueDateTime:O}'.",
                    Kind = EAuditKind.LogLineUpdated,
                };
            }
            else if (existingValue is TimeSpan existingTimeSpan && value is TimeSpan valueTimeSpan)
            {
                audit = new Audit
                {
                    DayFk     = existing.DayFk,
                    TimeStamp = now,
                    Source    = source?.GetType().FullName() ?? string.Empty,
                    Message = $"Changing {propertyInfo.Name} of {existing.Id} ({existing.TimeStamp}) " +
                              $"from '{existingTimeSpan:G}' to '{valueTimeSpan:G}'.",
                    Kind = EAuditKind.LogLineUpdated,
                };
            }
            else
            {
                audit = new Audit
                {
                    DayFk     = existing.DayFk,
                    TimeStamp = now,
                    Source    = source?.GetType().FullName() ?? string.Empty,
                    Message = $"Changing {propertyInfo.Name} of {existing.Id} ({existing.TimeStamp}) " +
                              $"from '{existingValue}' to '{value}'.",
                    Kind = EAuditKind.LogLineUpdated,
                };
            }

            await context.Audits.AddAsync(audit, cancellationToken)
                .ConfigureAwait(false);
            propertyInfo.SetValue(existing, value);
        }

        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task UpdateAsync(
        this Location location,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var now = DateTime.Now;
        var existing = await context.Locations.SingleOrDefaultAsync((q) => q.Id == location.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new ArgumentException("The provided Location could not be found in the database.");
        existing.Title = location.Title;
        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task UpdateAsync(
        this Project project,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var now = DateTime.Now;
        var existing = await context.Locations.SingleOrDefaultAsync((q) => q.Id == project.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new ArgumentException("The provided Project could not be found in the database.");
        existing.Title = project.Title;
        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }


    public static async Task<Day?> GetRelativeDayAsync(
        this Day day,
        [ValueRange(int.MinValue, -1)] int offset,
        CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        var query = context.Days
            .Where(
                (q) => q.Date.Year < day.Date.Year
                       || q.Date.Year == day.Date.Year && q.Date.Month <= day.Date.Month
                       || q.Date.Year == day.Date.Year && q.Date.Month == day.Date.Month && q.Date.Day <= day.Date.Day)
            .OrderByDescending((q) => q.Date.Year)
            .ThenByDescending((q) => q.Date.Month)
            .ThenByDescending((q) => q.Date.Day);
        var single = await query.Skip(Math.Abs(offset)).Take(1).SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<Day> GetTodayAsync(object? source, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var dateOnly = today.ToDateOnly();
        return await dateOnly.GetDayAsync(source, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task DeleteAsync(this TimeLog timeLog, object? source, CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        var day = await context.Days
            .SingleOrDefaultAsync((q) => q.Id == timeLog.DayFk, cancellationToken)
            .ConfigureAwait(false);
        if (day is null)
            throw new ArgumentException(
                "The provided TimeLog.Day could not be found in the database.",
                nameof(timeLog));
        var existing = await context.TimeLogs
            .Include((e) => e.Project)
            .Include((e) => e.Location)
            .SingleOrDefaultAsync((q) => q.Id == timeLog.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            throw new ArgumentException("The provided TimeLog could not be found in the database.", nameof(timeLog));
        var audit = new Audit
        {
            Source    = source?.GetType().FullName() ?? string.Empty,
            DayFk     = day.Id,
            TimeStamp = DateTime.Now,
            Message =
                $"Removing TimeLog {existing.Id} ({existing.TimeStamp}) with location '{existing.Location!.Title}' " +
                $"({existing.Location.Id}), project '{existing.Project!.Title}' ({existing.Project.Id}) and message '{existing.Message}'.",
            Kind = EAuditKind.LogLineRemoved,
        };
        context.TimeLogs.Remove(existing);
        await context.Audits
            .AddAsync(audit, cancellationToken)
            .ConfigureAwait(false);
        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    
    public static async Task<IReadOnlyCollection<(int id, string realm)>> GetJsonAttachmentRealmsAsync<T>(this T self,
        CancellationToken cancellationToken = default)
        where T : class, IHasJsonAttachment<T>, IHasId
    {
        await using var context = CreateContext();
        var set = context.Set<JsonAttachment<T>>();
        var parentId = self.Id;
        var range = await set
            .Where((q) => q.ParentFk == parentId)
            .Select((q) => new {q.Id, q.Realm})
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return range.Select((q) => (q.Id, q.Realm)).ToImmutableArray();
    }

    public static async Task<JsonAttachment<T>> GetJsonAttachmentAsync<T>(
        this T self,
        int id,
        CancellationToken cancellationToken = default)
        where T : class, IHasJsonAttachment<T>, IHasId, new()
    {
        await using var context = CreateContext();
        var set = context.Set<JsonAttachment<T>>();
        var parentId = self.Id;
        var single = await set
            .SingleOrDefaultAsync((q) => q.Id == id && q.ParentFk == parentId, cancellationToken)
            .ConfigureAwait(false);
        if (single is not null)
            return single;
        context.Set<T>().Attach(self);
        single = new JsonAttachment<T>()
        {
            Parent = self,
        };
        set.Add(single);
        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task<JsonAttachment<T>> GetJsonAttachmentAsync<T>(
        this T self,
        string realm,
        CancellationToken cancellationToken = default)
        where T : class, IHasJsonAttachment<T>, IHasId, new()
    {
        await using var context = CreateContext();
        var set = context.Set<JsonAttachment<T>>();
        var parentId = self.Id;
        var single = await set
            .SingleOrDefaultAsync((q) => q.Realm == realm && q.ParentFk == parentId, cancellationToken)
            .ConfigureAwait(false);
        if (single is not null)
            return single;
        context.Set<T>().Attach(self);
        single = new JsonAttachment<T>()
        {
            Realm  = realm,
            Parent = self,
        };
        set.Add(single);
        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return single;
    }

    public static async Task UpdateAsync<T>(this JsonAttachment<T> self, CancellationToken cancellationToken)
        where T : class, IHasJsonAttachment<T>, IHasId
    {
        await using var context = CreateContext();
        var set = context.Set<JsonAttachment<T>>();
        var realm = self.Realm;
        var parentId = self.ParentFk;
        var single = await set
            .SingleOrDefaultAsync((q) => q.Realm == realm && q.ParentFk == parentId, cancellationToken)
            .ConfigureAwait(false);
        if (single is null)
            throw new NullReferenceException("Failed to locate JsonAttachment");
        single.Json = self.Json;
        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}