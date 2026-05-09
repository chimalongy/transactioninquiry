using Microsoft.EntityFrameworkCore;
using transactioninquiry.Models;

namespace transactioninquiry.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TransactionInquiryUser> TransactionInquiryUsers { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ScriptDatabase> ScriptDatabases { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionInquiryUser>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Privileges)
                  .HasColumnName("privileges");
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Time)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ScriptDatabase>(entity =>
        {
            entity.Property(s => s.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        base.OnModelCreating(modelBuilder);
    }
}