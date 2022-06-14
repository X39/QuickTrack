using X39.Util;
using X39.Util.Collections;
using X39.Util.Console;

namespace QuickTrack;

public class QuickTrackHost
{
    private TimeLogLine? _lastMessage;
    private readonly List<TimeLogFile> _logFiles;
    private readonly string _workspace;
    private bool _isBreak;

    public QuickTrackHost(string workspace, List<TimeLogFile> logFiles, TimeLogLine? lastTimeLogLine)
    {
        _workspace = workspace;
        _logFiles = logFiles;
        _lastMessage = lastTimeLogLine;
    }

    public void StartBreak()
    {
        _isBreak = true;
        TryAppendNewLogLine("break:start");
    }

    public bool TryAppendNewLogLine(string line)
    {
        if (line.IsNullOrWhiteSpace())
        {
            if (_isBreak)
            {
                _isBreak = false;
                var msg = _lastMessage ?? throw new NullReferenceException("LastMessage is null");
                line = $"{msg.Project}:{msg.Message}";
            }
            else
            {
                new ConsoleString(
                    $"Empty line cannot be submitted.")
                {
                    Foreground = ConsoleColor.White,
                    Background = ConsoleColor.Red,
                }.WriteLine();
                return default;
            }
        }

        var splatted = line.Split(":");
        if (splatted.Length == 1 && _lastMessage is not null)
        {
            splatted = new[] {_lastMessage.Project, splatted[0]};
        }

        if (splatted.Length <= 1)
            return false;
        var now = DateTime.Now.ToUniversalTime();
        var today = now.ToDateOnly();
        var lastLogFile = _logFiles.FirstOrDefault((q) => q.Date == today)
                          ?? _logFiles.AddAndReturn(
                              new TimeLogFile(Path.Combine(_workspace, DateTime.Today.ToString("yyyy-MM-dd")),
                                  today));
        var tmp = new TimeLogLine(now, default, splatted[0].Trim(), string.Join(":", splatted.Skip(1)).Trim());
        lastLogFile.Append(tmp);
        if (_isBreak)
        {
            Programm.PrintBreakMessage(now, now.AddMinutes(30));
        }
        else
        {
            Programm.Print(tmp);
            _lastMessage = tmp;
        }

        return true;
    }
}