using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Infrastructure.Data;

namespace TimeQuest.UnitTests.TestHelpers;

/// <summary>
/// Creates an in-memory SQLite ApplicationDbContext for unit tests.
/// Each call creates a fresh connection and schema.
/// SQLite does not support RowVersion/TIMESTAMP — those columns are excluded from NOT NULL enforcement.
/// </summary>
public static class DbContextFactory
{
    public static ApplicationDbContext NewDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TestApplicationDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }
}

/// <summary>
/// ApplicationDbContext subclass that overrides RowVersion configuration for SQLite compatibility.
/// RowVersion/[Timestamp] columns are configured as nullable in SQLite to allow EnsureCreated() to succeed.
/// </summary>
public class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite does not support RowVersion/TIMESTAMP — mark as nullable so EnsureCreated succeeds
        modelBuilder.Entity<TimeQuest.Domain.Entities.WeeklyTimesheet>()
            .Property(wt => wt.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        modelBuilder.Entity<TimeQuest.Domain.Entities.Team>()
            .Property(t => t.RowVersion)
            .IsRowVersion()
            .IsRequired(false);
    }
}
