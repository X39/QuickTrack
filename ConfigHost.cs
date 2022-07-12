using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using X39.Util;
using X39.Util.Collections;

namespace QuickTrack;

public class ConfigHost
{
    private readonly string _filePath;

    public readonly Dictionary<(string? realm, string key), string> Values = new();
    private         bool                                            _wasLoaded;

    public ConfigHost(string filePath)
    {
        _filePath = filePath;
    }

    public void Load()
    {
        Values.Clear();
        _wasLoaded = true;
        if (!File.Exists(_filePath))
            return;
        using var fileStream = new FileStream(_filePath, FileMode.Open);
        using var streamReader = new StreamReader(fileStream);
        while (streamReader.ReadLine() is { } line)
        {
            var realmSplit = line.IndexOf('@');
            var valueSplit = line.IndexOf('=');
            if (realmSplit is -1 || realmSplit > valueSplit)
            {
                realmSplit = 0;
                var key = line[realmSplit..valueSplit];
                valueSplit++;
                var value = line[valueSplit..];
                Values.Add((null, key), value);
            }
            else
            {
                var realm = line[..realmSplit];
                realmSplit++;
                var key = line[realmSplit..valueSplit];
                valueSplit++;
                var value = line[valueSplit..];
                Values.Add((realm, key), value);
            }
        }
    }

    private void LoadIfWasNotLoaded()
    {
        if (_wasLoaded)
            return;
        Load();
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateRealm(string realm)
    {
        if (realm is not null && realm.IndexOfAny(new[] {'@', '='}) is not -1)
            throw new ValidationException("Realm may not contain either '@' or '='.");
    }
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateKey(string key)
    {
        if (key.IndexOfAny(new[] {'@', '='}) is not -1)
            throw new ValidationException("Key may not contain either '@' or '='.");
    }
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FileString(string? realm, string key, string value)
    {
        ValidateRealm(key);
        ValidateKey(key);
        return realm is null ? $"{key}={value}" : $"{realm}@{key}={value}";
    }

    public void Save()
    {
        using var fileStream = new FileStream(_filePath, FileMode.Create);
        using var streamWriter = new StreamWriter(fileStream);
        foreach (var value in Values)
        {
            streamWriter.WriteLine(FileString(value.Key.realm, value.Key.key, value.Value));
        }
    }

    public bool TryGet(Type type, string? realm, string key, [MaybeNullWhen(false)] out object value)
    {
        LoadIfWasNotLoaded();
        value = null;
        if (!Values.TryGetValue((realm, key), out var valueString))
            return false;
        value = System.Text.Json.JsonSerializer.Deserialize(valueString, type);
        return value is not null;
    }

    public bool TryGet<T>(string? realm, string key, [MaybeNullWhen(false)] out T value)
    {
        LoadIfWasNotLoaded();
        value = default;
        if (!Values.TryGetValue((realm, key), out var valueString))
            return false;
        value = System.Text.Json.JsonSerializer.Deserialize<T>(valueString);
        return value is not null;
    }

    public T? Get<T>(string? realm, string key, T? @default)
    {
        LoadIfWasNotLoaded();
        return Values.TryGetValue((realm, key), out var value)
            ? System.Text.Json.JsonSerializer.Deserialize<T>(value)
            : @default;
    }

    public T? Get<T>(string? realm, string key, Func<T?>? @default = null)
    {
        LoadIfWasNotLoaded();
        @default ??= () => default;
        if (Values.TryGetValue((realm, key), out var value))
        {
            Console.WriteLine(value);
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(value);
            Console.WriteLine(result?.ToString() ?? "null");
            return result;
        }
        else
            return @default();
    }


    public void Set<T>(string? realm, string key, T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        Values[(realm, key)] = json;
        Save();
    }

    public T Bind<T>(string? realm)
        where T : class
    {
        LoadIfWasNotLoaded();
        var type = typeof(T);
        var bound = type.CreateInstance<T>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.SetMethod is null)
                continue;
            if (!TryGet(propertyInfo.PropertyType, realm, propertyInfo.Name, out var value))
                continue;
            propertyInfo.SetValue(bound, value);
        }

        return bound;
    }
    public static string BindingTemplate<T>(string? realm)
        where T : class
    {
        var type = typeof(T);
        var builder = new StringBuilder();
        var bound = type.CreateInstance<T>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.SetMethod is null)
                continue;
            if (propertyInfo.GetMethod is null)
                throw new NullReferenceException($"Property {propertyInfo.Name} of {type.FullName()} has no getter");
            var defaultValue = propertyInfo.GetValue(bound);
            var value = System.Text.Json.JsonSerializer.Serialize(defaultValue);
            builder.AppendLine(FileString(realm, propertyInfo.Name, value));
        }

        return builder.ToString();
    }
}