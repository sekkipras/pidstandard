namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents a piping line in the P&ID
    /// </summary>
    public class Line
    {
        public Guid LineId { get; set; }
        public Guid ProjectId { get; set; }

        public string LineNumber { get; set; } = string.Empty;
        public string? Service { get; set; }
        public string? FluidType { get; set; }
        public string? NominalSize { get; set; }

        public Guid? LineSpecId { get; set; }
        public string? MaterialSpec { get; set; }
        public string? PipeSchedule { get; set; }

        public decimal? DesignPressure { get; set; }
        public decimal? DesignTemperature { get; set; }

        public bool InsulationRequired { get; set; }
        public string? InsulationType { get; set; }
        public decimal? InsulationThickness { get; set; }

        public Guid? FromEquipmentId { get; set; }
        public Guid? ToEquipmentId { get; set; }
        public decimal? Length { get; set; }

        public Guid? DrawingId { get; set; }

        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual Equipment? FromEquipment { get; set; }
        public virtual Equipment? ToEquipment { get; set; }
        public virtual Drawing? Drawing { get; set; }
    }
}
