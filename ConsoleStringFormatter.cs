using QuickTrack.Data.Database;
using QuickTrack.Data.EntityFramework;
using X39.Util.Collections.Concurrent;

namespace QuickTrack;

public class ConsoleStringFormatter : IAsyncDisposable
{
    private readonly RWLConcurrentDictionary<int, Project> _projects = new();
    private readonly RWLConcurrentDictionary<int, Location> _locations = new();


    internal async Task<Project> GetProjectAsync(int id, CancellationToken cancellationToken)
    {
        if (_projects.TryGetValue(id, out var project))
            return project;
        project = await QtRepository.GetProjectOrDefaultAsync(id, cancellationToken)
                  ?? throw new KeyNotFoundException($"Failed to locate project with id {id}");
        _projects.TryAdd(id, project);
        return project;
    }


    internal async Task<Location> GetLocationAsync(int id, CancellationToken cancellationToken)
    {
        if (_locations.TryGetValue(id, out var location))
            return location;
        location = await QtRepository.GetLocationOrDefaultAsync(id, cancellationToken)
                  ?? throw new KeyNotFoundException($"Failed to locate location with id {id}");
        _locations.TryAdd(id, location);
        return location;
    }

    public async ValueTask DisposeAsync()
    {
        await _projects.DisposeAsync();
    }

    public async Task<string> ToConsoleInputStringWithLocationAsync(TimeLog timeLog, CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(timeLog.ProjectFk, cancellationToken)
            .ConfigureAwait(false);
        var location = await GetLocationAsync(timeLog.LocationFk, cancellationToken)
            .ConfigureAwait(false);
        return $"[{location.Title}] {project.Title}: {timeLog.Message}";
    }

    public async Task<string> ToConsoleInputStringAsync(TimeLog timeLog, CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(timeLog.ProjectFk, cancellationToken)
            .ConfigureAwait(false);
        return $"{project.Title}: {timeLog.Message}";
    }

    public static string ToConsoleOutputPrefixString(Day today)
    {
        var tag = (DateTime.Today - today.Date).Days switch
        {
            0 => "[TOD]",
            1 => "[YST]",
            _ => today.Date.Date.DayOfWeek switch
            {
                DayOfWeek.Monday    => "[MON]",
                DayOfWeek.Tuesday   => "[TUE]",
                DayOfWeek.Wednesday => "[WED]",
                DayOfWeek.Thursday  => "[THU]",
                DayOfWeek.Friday    => "[FRI]",
                DayOfWeek.Saturday  => "[SAT]",
                DayOfWeek.Sunday    => "[SUN]",
                _                   => throw new ArgumentOutOfRangeException(),
            },
        };
        return $"[{today.Date:dd.MM}]{tag}";
    }
}