using System.Reflection;
using QuickTrack.Data.Database;

namespace QuickTrack.Data.Meta;

public interface IHasJsonAttachment<T> where T : class, IHasJsonAttachment<T>, IHasId
{
    ICollection<JsonAttachment<T>>? JsonAttachments { get; set; }
}

public interface IHasId
{
    public int Id { get; set; }
}