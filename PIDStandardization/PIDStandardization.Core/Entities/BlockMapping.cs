namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents a learned mapping between AutoCAD block names and equipment types
    /// Used for auto-suggesting equipment types during tag extraction
    /// </summary>
    public class BlockMapping
    {
        public Guid BlockMappingId { get; set; }

        /// <summary>
        /// AutoCAD block name (e.g., "PUMP-CENTRIFUGAL", "VALVE-GATE")
        /// </summary>
        public string BlockName { get; set; } = string.Empty;

        /// <summary>
        /// Equipment type (e.g., "Pump", "Valve", "Tank")
        /// </summary>
        public string EquipmentType { get; set; } = string.Empty;

        /// <summary>
        /// Number of times this mapping has been used
        /// </summary>
        public int UsageCount { get; set; } = 1;

        /// <summary>
        /// First time this mapping was created
        /// </summary>
        public DateTime FirstUsedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time this mapping was used
        /// </summary>
        public DateTime LastUsedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Confidence score (0.0 to 1.0)
        /// Calculated based on usage count and user confirmation
        /// </summary>
        public double ConfidenceScore { get; set; } = 0.5;

        /// <summary>
        /// User who created or confirmed this mapping
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Whether this mapping was manually confirmed by a user
        /// </summary>
        public bool IsUserConfirmed { get; set; } = false;
    }
}
