using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QuickTrack.Data.Meta;

namespace QuickTrack.Data.Database;

public class Day : IHasJsonAttachment<Day>, IHasId
{
    [Key]
    public int Id { get; set; }

    [Required] public DateComplex Date { get; set; } = DateComplex.Today;
    
    [InverseProperty(nameof(TimeLog.Day))]
    public ICollection<TimeLog>? TimeLogs { get; set; }
    
    [InverseProperty(nameof(Audit.Day))]
    public ICollection<Audit>? AuditLog { get; set; }

    public ICollection<JsonAttachment<Day>>? JsonAttachments { get; set; }


}