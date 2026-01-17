namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents an instrument (transmitter, indicator, controller, etc.)
    /// </summary>
    public class Instrument
    {
        public Guid InstrumentId { get; set; }
        public Guid ProjectId { get; set; }

        public string TagNumber { get; set; } = string.Empty;
        public string? InstrumentType { get; set; }
        public string? MeasurementType { get; set; }
        public string? ProcessConnection { get; set; }

        public decimal? RangeMin { get; set; }
        public decimal? RangeMax { get; set; }
        public string? Units { get; set; }
        public string? Accuracy { get; set; }

        public string? OutputSignal { get; set; }
        public string? LoopNumber { get; set; }
        public string? Location { get; set; }

        public Guid? ParentEquipmentId { get; set; }

        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public int? CalibrationFrequency { get; set; }
        public DateTime? LastCalibrationDate { get; set; }

        public Guid? DrawingId { get; set; }

        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual Equipment? ParentEquipment { get; set; }
        public virtual Drawing? Drawing { get; set; }
    }
}
