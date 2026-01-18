namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Audit log entry for tracking changes to equipment, lines, and instruments
    /// </summary>
    public class AuditLog
    {
        public Guid AuditLogId { get; set; }

        /// <summary>
        /// Type of entity being audited (Equipment, Line, Instrument, etc.)
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the entity being audited
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Action performed (Created, Updated, Deleted, Tagged, Synchronized, etc.)
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// User or system that performed the action
        /// </summary>
        public string PerformedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date and time of the action
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Details of what changed (JSON format or description)
        /// </summary>
        public string ChangeDetails { get; set; } = string.Empty;

        /// <summary>
        /// Old values before the change (JSON format)
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// New values after the change (JSON format)
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// Project associated with this audit entry
        /// </summary>
        public Guid? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        /// <summary>
        /// IP address or machine name where change was made
        /// </summary>
        public string? Source { get; set; }
    }
}
