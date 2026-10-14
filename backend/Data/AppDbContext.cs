using Microsoft.EntityFrameworkCore;
using WorkRequestTracker.Api.Models;

namespace WorkRequestTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkRequest> WorkRequests => Set<WorkRequest>();
    public DbSet<WorkRequestNote> Notes => Set<WorkRequestNote>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<WorkRequest>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.ClientName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.Status, x.DueDate });
            e.HasIndex(x => x.ClientName);
            e.HasIndex(x => x.CreatedAt);
            e.HasMany(x => x.Notes).WithOne().HasForeignKey(n => n.WorkRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WorkRequestNote>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.WorkRequestId, x.CreatedAt });
        });
    }
}
