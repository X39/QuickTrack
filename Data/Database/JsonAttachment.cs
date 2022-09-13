using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuickTrack.Data.Meta;
using X39.Util;

namespace QuickTrack.Data.Database;

[Index(nameof(Realm), nameof(ParentFk), IsUnique = true)]
public class JsonAttachment<T> : IHasId where T : class, IHasJsonAttachment<T>, IHasId
{
    [Key] public int Id { get; set; }
    [ForeignKey(nameof(ParentFk))] public T? Parent { get; set; }
    public int ParentFk { get; set; }
    public string Realm { get; init; } = string.Empty;

    public string Json { get; set; } = string.Empty;


    public async Task WithDoAsync<TData>(
        Func<TData, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        TData t;
        if (Json.IsNullOrWhiteSpace())
        {
            t = new TData();
        }
        else
        {
            using var inDoc = JsonDocument.Parse(Json);
            t = inDoc.Deserialize<TData>() ?? new TData();
        }

        await func(t, cancellationToken);

        using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, t, cancellationToken: cancellationToken);
        Json = Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    public async Task<TResult> WithDoAsync<TData, TResult>(
        Func<TData, CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        TData t;
        if (Json.IsNullOrWhiteSpace())
        {
            t = new TData();
        }
        else
        {
            using var inDoc = JsonDocument.Parse(Json);
            t = inDoc.Deserialize<TData>() ?? new TData();
        }

        var result = await func(t, cancellationToken);

        using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, t, cancellationToken: cancellationToken);
        Json = Encoding.UTF8.GetString(memoryStream.ToArray());
        return result;
    }
}