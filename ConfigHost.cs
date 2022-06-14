using System.Text.Json.Serialization;

namespace QuickTrack;

public class ConfigHost
{
    private readonly string _workspace;
    private string FilePath => Path.Combine(_workspace, "config.cfg");

    public readonly Dictionary<(string realm, string key), string> _values = new();
    private bool _wasLoaded;

    public ConfigHost(string workspace)
    {
        _workspace = workspace;
    }

    public void Load()
    {
        _values.Clear();
        _wasLoaded = true;
        if (!File.Exists(FilePath))
            return;
        using var fileStream = new FileStream(FilePath, FileMode.Open);
        using var streamReader = new StreamReader(fileStream);
        while (streamReader.ReadLine() is { } line)
        {
            var realmSplit = line.IndexOf('@');
            var valueSplit = line.IndexOf('=');
            var realm = line[..realmSplit];
            realmSplit++;
            var key = line[realmSplit..valueSplit];
            valueSplit++;
            var value = line[valueSplit..];
            _values.Add((realm, key), value);
        }
    }
    private void LoadIfWasNotLoaded()
    {
        if (_wasLoaded)
            return;
        Load();
    }

    public void Save()
    {
        using var fileStream = new FileStream(FilePath, FileMode.Create);
        using var streamWriter = new StreamWriter(fileStream);
        foreach (var value in _values)
        {
            streamWriter.WriteLine($"{value.Key.realm}@{value.Key.key}={value.Value}");
        }
    }

    public T? Get<T>(string realm, string key, T? @default)
    {
        LoadIfWasNotLoaded();
        return _values.TryGetValue((realm, key), out var value)
            ? System.Text.Json.JsonSerializer.Deserialize<T>(value)
            : @default;
    }
    public T? Get<T>(string realm, string key, Func<T?>? @default = null)
    {
        LoadIfWasNotLoaded();
        @default ??= () => default;
        if (_values.TryGetValue((realm, key), out var value))
        {
            Console.WriteLine(value);
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(value);
            Console.WriteLine(result?.ToString() ?? "null");
            return result;
        }
        else
            return @default();
    }


    public void Set<T>(string realm, string key, T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        _values[(realm, key)] = json;
        Save();
    }
}