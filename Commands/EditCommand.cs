using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Fastenshtein;
using JetBrains.Annotations;
using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack.Commands;

public class EditCommand : IConsoleCommand
{
    public string[] Keys { get; } = {"edit", "modify", "nano"};

    public string Description =>
        "Opens a log file in the text-editor of your choice. " +
        $"Dates are to be provided in the format {Constants.DateFormatFormal} or {Constants.DateFormatNoDot}" +
        $" (eg. 21.02 or 2102)";

    public string Pattern => "( edit | modify | nano ) DATE { DATE }";

    public async ValueTask ExecuteAsync(ImmutableArray<string> args, CancellationToken cancellationToken)
    {
        var dates = new DateOnly[args.Length];
        var error = false;
        if (args.Length is 0)
        {
            new ConsoleString(
                $"No date provided. Please provide a date in the format " +
                $"{Constants.DateFormatFormal} or {Constants.DateFormatNoDot}")
            {
                Foreground = ConsoleColor.Red,
                Background = ConsoleColor.Black,
            }.WriteLine();
            return;
        }

        for (var i = 0; i < args.Length; i++)
        {
            var dateString = args[i];
            if (DateOnly.TryParseExact(
                    dateString,
                    new[] {Constants.DateFormatFormal, Constants.DateFormatNoDot},
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var date))
                dates[i] = date;
            else
            {
                new ConsoleString($"Failed to parse date string '{dateString}'")
                {
                    Foreground = ConsoleColor.Red,
                    Background = ConsoleColor.Black,
                }.WriteLine();
                error = true;
            }
        }

        if (error)
            return;

        var logs = await GetLogLinesAsync(dates, cancellationToken);

        var tempFile = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            var cache = new Cache(cancellationToken);
            await SerializeToCsvAsync(cache, logs, tempFile, cancellationToken);
            var lastWriteTime = File.GetLastWriteTime(tempFile);
            bool reopen;
            do
            {
                reopen = false;
                await OpenInEditorAsync(tempFile, cancellationToken);
                if (File.GetLastWriteTime(tempFile) <= lastWriteTime)
                    continue;
                var result = await UpdateFromCsvAsync(cache, logs, tempFile, cancellationToken);
                if (result is not EMergeResult.Reopen)
                    continue;
                reopen        = true;
                lastWriteTime = File.GetLastWriteTime(tempFile);
            } while (reopen);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private async Task<ImmutableArray<TimeLog>> GetLogLinesAsync(
        IEnumerable<DateOnly> dates,
        CancellationToken cancellationToken)
    {
        var timeLogs = new List<TimeLog>();
        foreach (var date in dates)
        {
            var day = await date.GetDayOrDefaultAsync(cancellationToken);
            if (day is null)
                continue;
            var timeLogsOfDay = await day.GetTimeLogsAsync(cancellationToken);
            timeLogs.AddRange(timeLogsOfDay);
        }

        return timeLogs.ToImmutableArray();
    }

    private async Task SerializeToCsvAsync(
        Cache cache,
        ImmutableArray<TimeLog> logs,
        string tempFile,
        CancellationToken cancellationToken)
    {
        var csvRows = new List<CsvRow>();
        foreach (var dayFkGroup in logs.GroupBy((q) => q.DayFk))
        {
            var day = await QtRepository.GetDayOrDefaultAsync(dayFkGroup.Key, cancellationToken)
                      ?? throw new NullReferenceException($"Failed to get day with id {dayFkGroup.Key}.");
            var previousTimeOnly = default(TimeOnly?);
            foreach (var timeLog in dayFkGroup.Reverse())
            {
                var project = await cache.GetProjectAsync(timeLog.ProjectFk);
                var location = await cache.GetLocationAsync(timeLog.LocationFk);
                csvRows.Add(
                    new CsvRow
                    {
                        Date     = day.Date,
                        Start    = timeLog.TimeStamp.ToTimeOnly(),
                        End      = previousTimeOnly,
                        Location = location.Title,
                        Project  = project.Title,
                        Message  = timeLog.Message,
                        Mode     = timeLog.Mode,
                    });
                previousTimeOnly = timeLog.TimeStamp.ToTimeOnly();
            }
        }

        csvRows.Sort((l, r) => (l.Date, l.Start).CompareTo((r.Date, r.Start)));
        await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
        await using var writer = new CsvWriter(streamWriter, CultureInfo.InstalledUICulture);
        await writer.WriteRecordsAsync(csvRows, cancellationToken);
    }

    private static async Task OpenInEditorAsync(string tempFile, CancellationToken cancellationToken)
    {
        // .\notepad++.exe -multiInst -notabbar -nosession .\shortcuts.xml
        var process = Process.Start(
                          new ProcessStartInfo(tempFile)
                          {
                              UseShellExecute = true,
                          })
                      ?? throw new NullReferenceException("Failed to get process from Process.Start.");
        await process.WaitForExitAsync(cancellationToken);
        if (!process.HasExited)
            process.Kill();
    }

    private Task<EMergeResult> UpdateFromCsvAsync(
        Cache cache,
        ImmutableArray<TimeLog> logs,
        string tempFile,
        CancellationToken cancellationToken)
    {
        var merger = new CsvDataMerger(cache, logs, tempFile, cancellationToken);
        return merger.MergeAsync();
    }

    private class Cache
    {
        public Cache(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        private readonly Dictionary<int, Day>      _days      = new();
        private readonly Dictionary<int, Project>  _projects  = new();
        private readonly Dictionary<int, Location> _locations = new();
        private readonly CancellationToken         _cancellationToken;

        public async ValueTask<Project> GetProjectAsync(int id)
        {
            if (_projects.TryGetValue(id, out var project))
                return project;
            project = await QtRepository.GetProjectOrDefaultAsync(id, _cancellationToken)
                      ?? throw new NullReferenceException($"Failed to get project with id {id}.");
            return _projects[id] = project;
        }

        public async ValueTask<Location> GetLocationAsync(int id)
        {
            if (_locations.TryGetValue(id, out var location))
                return location;
            location = await QtRepository.GetLocationOrDefaultAsync(id, _cancellationToken)
                       ?? throw new NullReferenceException($"Failed to get location with id {id}.");
            return _locations[id] = location;
        }

        public async ValueTask<Day> GetDayAsync(int id)
        {
            if (_days.TryGetValue(id, out var day))
                return day;
            day = await QtRepository.GetDayOrDefaultAsync(id, _cancellationToken)
                  ?? throw new NullReferenceException($"Failed to get day with id {id}.");
            return _days[id] = day;
        }

        public async ValueTask<Project?> GetProjectOrDefaultAsync(int id)
        {
            if (_projects.TryGetValue(id, out var project))
                return project;
            project = await QtRepository.GetProjectOrDefaultAsync(id, _cancellationToken);
            if (project is null)
                return null;
            return _projects[id] = project;
        }

        public async ValueTask<Location?> GetLocationOrDefaultAsync(int id)
        {
            if (_locations.TryGetValue(id, out var location))
                return location;
            location = await QtRepository.GetLocationOrDefaultAsync(id, _cancellationToken);
            if (location is null)
                return null;
            return _locations[id] = location;
        }

        public async ValueTask<Day?> GetDayOrDefaultAsync(int id)
        {
            if (_days.TryGetValue(id, out var day))
                return day;
            day = await QtRepository.GetDayOrDefaultAsync(id, _cancellationToken);
            if (day is null)
                return null;
            return _days[id] = day;
        }
    }

    private class CsvDataMerger
    {
        private readonly Cache                   _cache;
        private readonly ImmutableArray<TimeLog> _logs;
        private readonly string                  _tempFile;
        private readonly CancellationToken       _cancellationToken;

        public CsvDataMerger(
            Cache cache,
            ImmutableArray<TimeLog> logs,
            string tempFile,
            CancellationToken cancellationToken)
        {
            _cache             = cache;
            _logs              = logs;
            _tempFile          = tempFile;
            _cancellationToken = cancellationToken;
        }

        public async Task<EMergeResult> MergeAsync()
        {
            await using var fileStream = new FileStream(_tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
            using var reader = new CsvReader(
                streamReader,
                new CsvConfiguration(CultureInfo.InstalledUICulture)
                {
                    HasHeaderRecord  = true,
                    ShouldSkipRecord = ShouldSkipCsvRecord,
                });
            var oldToNewTimeLogMapping = _logs.ToDictionary(
                (q) => q,
                (_) => default((CsvRow row, TimeLog updatedTimeLog)?));
            var newTimeLogs = new List<(CsvRow row, TimeLog updatedTimeLog)>();
            await foreach (var row in reader.GetRecordsAsync<CsvRow>(_cancellationToken))
            {
                var bestCandidate = await GetBestCandidateOrDefaultAsync(oldToNewTimeLogMapping, row);
                var updatedTimeLog = bestCandidate.ShallowCopy();

                updatedTimeLog.Message   = row.Message;
                updatedTimeLog.TimeStamp = row.Date.ToDateTime(row.Start);
                updatedTimeLog.Mode      = row.Mode;

                var tuple = (row, updatedTimeLog);
                if (bestCandidate is null)
                    newTimeLogs.Add(tuple);
                else
                    oldToNewTimeLogMapping[bestCandidate] = tuple;
            }

            if (HasGaps(newTimeLogs.Concat(oldToNewTimeLogMapping.Values.NotNull())))
            {
                new ConsoleString("File has gaps for times, prompting reopen (close immediately to abort).")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.DarkYellow,
                }.WriteLine();
                return EMergeResult.Reopen;
            }

            await DeleteTimeLogsAsync(
                oldToNewTimeLogMapping
                    .Where((q) => q.Value is null)
                    .Select((q) => q.Key));
            await UpdateTimeLogsAsync(
                oldToNewTimeLogMapping
                    .Where((q) => q.Value is not null));
            await CreateTimeLogsAsync(newTimeLogs);

            return EMergeResult.Success;
        }

        private bool ShouldSkipCsvRecord(ShouldSkipRecordArgs record)
        {
            for (var i = 0; true; i++)
            {
                if (!record.Row.TryGetField(i, out string str))
                    break;
                if (!str.IsNullOrWhiteSpace())
                    return false;
            }

            return true;
        }

        private static bool HasGaps(IEnumerable<(CsvRow row, TimeLog updatedTimeLog)> oldToNewTimeLogMapping)
        {
            (TimeOnly Start, TimeOnly End)? previous = default;
            var gap = TimeSpan.Zero;
            var data = oldToNewTimeLogMapping
                .Select((q) => (q.row.Start, End: q.row.End ?? q.row.Start))
                .OrderBy((q) => q.Start);
            foreach (var value in data)
            {
                if (previous is not null)
                    gap += value.Start - previous.Value.End;
                previous = value;
            }

            return gap > TimeSpan.Zero;
        }

        private async Task CreateTimeLogsAsync(IEnumerable<(CsvRow row, TimeLog updatedTimeLog)> timeLogs)
        {
            foreach (var (row, timeLog) in timeLogs)
            {
                var day = await row.Date.GetDayAsync(this, _cancellationToken);
                var project = await row.Project.Trim().GetProjectAsync(_cancellationToken);
                var location = await row.Location.Trim().GetLocationAsync(_cancellationToken);
                await day.AppendTimeLogAsync(
                    this,
                    location,
                    project,
                    timeLog.Mode,
                    timeLog.Message,
                    timeLog.TimeStamp,
                    _cancellationToken);
            }
        }

        private async Task UpdateTimeLogsAsync(
            IEnumerable<KeyValuePair<TimeLog, (CsvRow row, TimeLog updatedTimeLog)?>> timeLogs)
        {
            foreach (var (existing, tuple) in timeLogs)
            {
                if (tuple is null)
                    continue;
                var updated = tuple.Value.updatedTimeLog;
                updated.Id = existing.Id;
                await updated.UpdateAsync(this, _cancellationToken);
            }
        }

        private async Task DeleteTimeLogsAsync(IEnumerable<TimeLog> timeLogs)
        {
            foreach (var timeLog in timeLogs)
            {
                await timeLog.DeleteAsync(this, _cancellationToken);
            }
        }

        private async Task<TimeLog?> GetBestCandidateOrDefaultAsync(
            Dictionary<TimeLog, (CsvRow row, TimeLog updatedTimeLog)?> oldToNewTimeLogMapping,
            CsvRow row)
        {
            var candidates = await GatherCandidatesAsync(oldToNewTimeLogMapping, row);

            var bestCandidate = candidates
                .Where((q) => q.score > 2)
                .OrderBy((q) => q.score)
                .Select((q) => q.timeLog)
                .FirstOrDefault();
            return bestCandidate;
        }

        private async Task<List<(Day day, Project project, Location location, TimeLog timeLog, double score)>>
            GatherCandidatesAsync(
                Dictionary<TimeLog, (CsvRow row, TimeLog updatedTimeLog)?> oldToNewTimeLogMapping,
                CsvRow row)
        {
            var candidates = new List<(Day day, Project project, Location location, TimeLog timeLog, double score)>();
            TimeOnly? endTime = null;
            foreach (var timeLog in oldToNewTimeLogMapping
                         .Where((q) => q.Value is null)
                         .Select((q) => q.Key)
                         .Reverse())
            {
                var day = await _cache.GetDayAsync(timeLog.DayFk);
                var project = await _cache.GetProjectAsync(timeLog.ProjectFk);
                var location = await _cache.GetLocationAsync(timeLog.LocationFk);
                if (row.Date != day.Date)
                    continue;
                var startTime = timeLog.TimeStamp.ToTimeOnly();
                var score = 0.0;
                if (endTime == row.End)
                    score += endTime is null ? 2 : 1;
                if (startTime == row.Start)
                    score += 1;
                if (timeLog.Mode == row.Mode)
                    score += 1;
                score += LevenshteinScore(project.Title, row.Project.Trim(), 3);
                score += LevenshteinScore(location.Title, row.Location.Trim(), 3);
                score += LevenshteinScore(timeLog.Message, row.Message.Trim(), 10);
                candidates.Add((day, project, location, timeLog, score));
                endTime = startTime;
            }

            return candidates;
        }

        private static double LevenshteinScore(string left, string right, [ValueRange(1, int.MaxValue)] int impact)
        {
            var distance = Levenshtein.Distance(left, right);
            var score = (double) impact / distance;
            return Math.Min(1, score);
        }
    }


    private record CsvRow
    {
        public DateOnly Date { get; init; }
        [Format("HH:mm:ss", "HH:mm")] public TimeOnly Start { get; init; }
        [Format("HH:mm:ss", "HH:mm")] public TimeOnly? End { get; init; }

        public string Location { get; init; } = string.Empty;
        public string Project { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public ETimeLogMode Mode { get; init; }
    }

    private enum EMergeResult
    {
        Invalid,
        Success,
        Failure,
        Reopen,
    }
}