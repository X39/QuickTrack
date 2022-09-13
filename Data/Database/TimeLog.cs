using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuickTrack.Data.Meta;
using SQLitePCL;

namespace QuickTrack.Data.Database;

[Index(nameof(TimeStamp))]
[Index(nameof(Message))]
public class TimeLog : IHasJsonAttachment<TimeLog>, IHasId
{
    [Key] public int Id { get; set; }

    [ForeignKey(nameof(DayFk))] public Day? Day { get; set; }
    public int DayFk { get; set; }

    [ForeignKey(nameof(ProjectFk))] public Project? Project { get; set; }
    public int ProjectFk { get; set; }

    [ForeignKey(nameof(LocationFk))] public Location? Location { get; set; }
    public int LocationFk { get; set; }

    public DateTime TimeStamp { get; set; }

    public string Message { get; set; } = string.Empty;

    public ICollection<JsonAttachment<TimeLog>>? JsonAttachments { get; set; }
    public ETimeLogMode Mode { get; set; }

    public override string ToString()
    {
        return $"{nameof(TimeLog)} {{ {nameof(Id)}: {Id}, {nameof(Day)}: {Day}, {nameof(DayFk)}: {DayFk}, " +
               $"{nameof(Project)}: {Project}, {nameof(ProjectFk)}: {ProjectFk}, {nameof(Location)}: {Location}, " +
               $"{nameof(LocationFk)}: {LocationFk}, {nameof(TimeStamp)}: {TimeStamp}, {nameof(Message)}: {Message}," +
               $" {nameof(JsonAttachments)}: {JsonAttachments}, {nameof(Mode)}: {Mode} }}";
    }

    public TimeLog ShallowCopy()
    {
        return new TimeLog
        {
            Day             = null,
            Id              = Id,
            Location        = null,
            Message         = Message,
            Mode            = Mode,
            Project         = null,
            DayFk           = DayFk,
            JsonAttachments = null,
            LocationFk      = LocationFk,
            ProjectFk       = ProjectFk,
            TimeStamp       = TimeStamp,
        };
    }
}