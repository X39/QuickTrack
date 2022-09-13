using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuickTrack.Data.Meta;

namespace QuickTrack.Data.Database;

public class Audit : IHasId
{
    [Key] public int Id { get; set; }

    [ForeignKey(nameof(DayFk))] public Day? Day { get; set; }
    public int DayFk { get; set; }

    public DateTime TimeStamp { get; set; }

    public string Message { get; set; } = string.Empty;

    public EAuditKind Kind { get; set; }
    public string? Json { get; set; }
    public string Source { get; set; } = string.Empty;
}