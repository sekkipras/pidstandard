using PIDStandardization.Core.Enums;

namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents a piece of equipment (pump, valve, tank, etc.)
    /// Works with both Custom and KKS tagging modes
    /// </summary>
    public class Equipment
    {
        public Guid EquipmentId { get; set; }
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Unique tag number (e.g., P-100-PMP-001 or +LAA 10 CP001)
        /// </summary>
        public string TagNumber { get; set; } = string.Empty;

        public string? EquipmentType { get; set; }
        public string? Description { get; set; }
        public string? Service { get; set; }
        public string? Area { get; set; }

        public EquipmentStatus Status { get; set; } = EquipmentStatus.Planned;

        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? InstallationDate { get; set; }

        // Process Parameters - Operating Conditions
        public decimal? OperatingPressure { get; set; }
        public string? OperatingPressureUnit { get; set; } // bar, psi, kPa, MPa
        public decimal? OperatingTemperature { get; set; }
        public string? OperatingTemperatureUnit { get; set; } // C, F, K
        public decimal? FlowRate { get; set; }
        public string? FlowRateUnit { get; set; } // m3/h, L/min, gpm, kg/h

        // Process Parameters - Design Conditions
        public decimal? DesignPressure { get; set; }
        public string? DesignPressureUnit { get; set; }
        public decimal? DesignTemperature { get; set; }
        public string? DesignTemperatureUnit { get; set; }

        // Equipment Capacity/Power
        public decimal? PowerOrCapacity { get; set; }
        public string? PowerOrCapacityUnit { get; set; } // kW, HP, m3, L, tons

        // Equipment Connectivity (for valves, control points, etc.)
        public Guid? UpstreamEquipmentId { get; set; }
        public Guid? DownstreamEquipmentId { get; set; }

        /// <summary>
        /// JSON string for flexible attributes
        /// </summary>
        public string? SpecificationJson { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Source drawing tracking
        public Guid? DrawingId { get; set; }
        public string? SourceBlockName { get; set; }

        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual Drawing? SourceDrawing { get; set; }
        public virtual Equipment? UpstreamEquipment { get; set; }
        public virtual Equipment? DownstreamEquipment { get; set; }
        public virtual ICollection<Equipment>? DownstreamConnections { get; set; }
        public virtual ICollection<Equipment>? UpstreamConnections { get; set; }
    }
}
