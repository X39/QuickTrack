using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuickTrack.Data.Meta;

namespace QuickTrack.Data.Database;

[Index(nameof(Title), IsUnique = true)]
public class Project : IHasJsonAttachment<Project>, IHasId
{
    [Key] public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    [InverseProperty(nameof(TimeLog.Project))]
    public ICollection<TimeLog>? TimeLogs { get; set; }

    public ICollection<JsonAttachment<Project>>? JsonAttachments { get; set; }
    public DateTime TimeStampCreated { get; set; }
}