using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using QuickTrack.Data.Database;

namespace QuickTrack.Data.EntityFramework;

public class QtContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite(
            new SqliteConnectionStringBuilder()
            {
                DataSource = Path.GetFullPath(Constants.DatabaseFile),
            }.ToString());
    }

    public DbSet<Day> Days { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<TimeLog> TimeLogs { get; set; } = null!;
    public DbSet<Audit> Audits { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
}