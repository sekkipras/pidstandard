using PIDStandardization.Core.Enums;

namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents a P&ID project
    /// </summary>
    public class Project
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ProjectNumber { get; set; }
        public string? Client { get; set; }

        /// <summary>
        /// Tagging mode - Custom or KKS. Cannot be changed after tags are assigned.
        /// </summary>
        public TaggingMode TaggingMode { get; set; }

        public Guid? CustomFormatId { get; set; }
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
        public virtual ICollection<Drawing> Drawings { get; set; } = new List<Drawing>();
    }
}
