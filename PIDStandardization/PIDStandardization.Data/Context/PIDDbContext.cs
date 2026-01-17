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
        public DbSet<BlockMapping> BlockMappings { get; set; }

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
            ConfigureBlockMapping(modelBuilder);
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

        private void ConfigureBlockMapping(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockMapping>(entity =>
            {
                entity.HasKey(e => e.BlockMappingId);
                entity.Property(e => e.BlockName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EquipmentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);

                // Unique index on BlockName for fast lookup
                entity.HasIndex(e => e.BlockName).IsUnique();

                // Index on EquipmentType for reporting
                entity.HasIndex(e => e.EquipmentType);
            });
        }
    }
}
