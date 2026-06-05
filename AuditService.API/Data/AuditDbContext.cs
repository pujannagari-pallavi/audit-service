using AuditService.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditService.API.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

        public DbSet<Audit> Audits { get; set; }
        public DbSet<AuditAuditor> AuditAuditors { get; set; }
        public DbSet<AuditHistory> AuditHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Audit>(entity =>
            {
                entity.HasKey(a => a.AuditId);
                entity.Property(a => a.AuditCode).IsRequired().HasMaxLength(50);
                entity.HasIndex(a => a.AuditCode).IsUnique();
                entity.Property(a => a.AuditName).IsRequired().HasMaxLength(200);
                entity.Property(a => a.AuditType).HasConversion<int>();
                entity.Property(a => a.Status).HasConversion<int>();
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasQueryFilter(a => !a.IsDeleted);
            });

            modelBuilder.Entity<AuditAuditor>(entity =>
            {
                entity.HasKey(aa => aa.Id);
                entity.HasOne(aa => aa.Audit)
                      .WithMany(a => a.AuditAuditors)
                      .HasForeignKey(aa => aa.AuditId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditHistory>(entity =>
            {
                entity.HasKey(ah => ah.Id);
                entity.Property(ah => ah.FieldChanged).IsRequired().HasMaxLength(100);
                entity.Property(ah => ah.OldValue).HasMaxLength(500);
                entity.Property(ah => ah.NewValue).HasMaxLength(500);
                entity.Property(ah => ah.ChangedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(ah => ah.Audit)
                      .WithMany(a => a.AuditHistories)
                      .HasForeignKey(ah => ah.AuditId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
