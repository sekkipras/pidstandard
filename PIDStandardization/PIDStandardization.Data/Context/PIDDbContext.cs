using Microsoft.EntityFrameworkCore;
using PIDStandardization.Core.Entities;

namespace PIDStandardization.Data.Context
{
    /// <summary>
    /// Main database context for P&ID Standardization application
    /// </summary>
    public class PIDDbContext : DbContext
    {
        public PIDDbContext(DbContextOptions<PIDDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Project> Projects { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Drawing> Drawings { get; set; }
        public DbSet<Line> Lines { get; set; }
        public DbSet<Instrument> Instruments { get; set; }
        public DbSet<ValidationRule> ValidationRules { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            ConfigureProject(modelBuilder);
            ConfigureEquipment(modelBuilder);
            ConfigureDrawing(modelBuilder);
            ConfigureLine(modelBuilder);
            ConfigureInstrument(modelBuilder);
            ConfigureValidationRule(modelBuilder);
            ConfigureAuditLog(modelBuilder);
        }

        private void ConfigureProject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.ProjectId);
                entity.Property(e => e.ProjectName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ProjectNumber).HasMaxLength(50);
                entity.Property(e => e.Client).HasMaxLength(200);
                entity.Property(e => e.TaggingMode).IsRequired();

                entity.HasIndex(e => e.ProjectName);
            });
        }

        private void ConfigureEquipment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.EquipmentId);
                entity.Property(e => e.TagNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EquipmentType).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Service).HasMaxLength(200);
                entity.Property(e => e.Area).HasMaxLength(50);

                // Unique constraint on TagNumber per Project
                entity.HasIndex(e => new { e.ProjectId, e.TagNumber }).IsUnique();

                // Relationship with Project
                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Equipment)
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relationship with Drawing (source drawing for this equipment)
                entity.HasOne(e => e.SourceDrawing)
                      .WithMany(d => d.Equipment)
                      .HasForeignKey(e => e.DrawingId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.Property(e => e.SourceBlockName).HasMaxLength(100);

                // Process parameter configurations
                entity.Property(e => e.OperatingPressure).HasPrecision(18, 2);
                entity.Property(e => e.OperatingPressureUnit).HasMaxLength(20);
                entity.Property(e => e.OperatingTemperature).HasPrecision(18, 2);
                entity.Property(e => e.OperatingTemperatureUnit).HasMaxLength(20);
                entity.Property(e => e.FlowRate).HasPrecision(18, 2);
                entity.Property(e => e.FlowRateUnit).HasMaxLength(20);

                entity.Property(e => e.DesignPressure).HasPrecision(18, 2);
                entity.Property(e => e.DesignPressureUnit).HasMaxLength(20);
                entity.Property(e => e.DesignTemperature).HasPrecision(18, 2);
                entity.Property(e => e.DesignTemperatureUnit).HasMaxLength(20);

                entity.Property(e => e.PowerOrCapacity).HasPrecision(18, 2);
                entity.Property(e => e.PowerOrCapacityUnit).HasMaxLength(20);

                // Equipment connectivity relationships
                entity.HasOne(e => e.UpstreamEquipment)
                      .WithMany(e => e.DownstreamConnections)
                      .HasForeignKey(e => e.UpstreamEquipmentId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

                entity.HasOne(e => e.DownstreamEquipment)
                      .WithMany(e => e.UpstreamConnections)
                      .HasForeignKey(e => e.DownstreamEquipmentId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
            });
        }

        private void ConfigureDrawing(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Drawing>(entity =>
            {
                entity.HasKey(e => e.DrawingId);
                entity.Property(e => e.DrawingNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DrawingTitle).HasMaxLength(200);
                entity.Property(e => e.FilePath).HasMaxLength(500);

                // Unique constraint on DrawingNumber per Project
                entity.HasIndex(e => new { e.ProjectId, e.DrawingNumber }).IsUnique();

                // Relationship with Project
                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Drawings)
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Line>(entity =>
            {
                entity.HasKey(e => e.LineId);
                entity.Property(e => e.LineNumber).IsRequired().HasMaxLength(50);

                // Decimal precision for engineering values
                entity.Property(e => e.DesignPressure).HasPrecision(18, 4);
                entity.Property(e => e.DesignTemperature).HasPrecision(18, 4);
                entity.Property(e => e.Length).HasPrecision(18, 4);
                entity.Property(e => e.InsulationThickness).HasPrecision(18, 4);

                // Unit fields for design conditions
                entity.Property(e => e.DesignPressureUnit).HasMaxLength(20);
                entity.Property(e => e.DesignTemperatureUnit).HasMaxLength(20);

                // Unique constraint on LineNumber per Project
                entity.HasIndex(e => new { e.ProjectId, e.LineNumber }).IsUnique();

                // Relationships
                entity.HasOne(e => e.Project)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.FromEquipment)
                      .WithMany()
                      .HasForeignKey(e => e.FromEquipmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ToEquipment)
                      .WithMany()
                      .HasForeignKey(e => e.ToEquipmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureInstrument(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Instrument>(entity =>
            {
                entity.HasKey(e => e.InstrumentId);
                entity.Property(e => e.TagNumber).IsRequired().HasMaxLength(50);

                // Decimal precision for range values
                entity.Property(e => e.RangeMin).HasPrecision(18, 4);
                entity.Property(e => e.RangeMax).HasPrecision(18, 4);

                // Unique constraint on TagNumber per Project
                entity.HasIndex(e => new { e.ProjectId, e.TagNumber }).IsUnique();

                // Relationships
                entity.HasOne(e => e.Project)
                      .WithMany()
                      .HasForeignKey(e => e.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ParentEquipment)
                      .WithMany()
                      .HasForeignKey(e => e.ParentEquipmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Line relationship (instrument can be installed on a line)
                entity.HasOne(e => e.Line)
                      .WithMany()
                      .HasForeignKey(e => e.LineId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureValidationRule(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ValidationRule>(entity =>
            {
                entity.HasKey(e => e.RuleId);
                entity.Property(e => e.RuleCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);

                entity.HasIndex(e => e.RuleCode).IsUnique();
            });
        }

        private void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditLogId);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.ChangeDetails).HasMaxLength(2000);
                entity.Property(e => e.Source).HasMaxLength(100);

                // Indexes for common queries
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.Timestamp });

                // Foreign key relationship to Project
                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
