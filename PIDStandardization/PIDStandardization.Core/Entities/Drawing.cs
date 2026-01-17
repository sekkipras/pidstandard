namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents a P&ID drawing file
    /// </summary>
    public class Drawing
    {
        public Guid DrawingId { get; set; }
        public Guid ProjectId { get; set; }

        public string DrawingNumber { get; set; } = string.Empty;
        public string? DrawingTitle { get; set; }
        public string? FilePath { get; set; }              // Original file path
        public string? Revision { get; set; }
        public DateTime? RevisionDate { get; set; }
        public string? Status { get; set; }

        // New fields for file management and versioning
        public string? FileName { get; set; }              // Original filename
        public string? StoredFilePath { get; set; }        // Path to copied file in app storage
        public DateTime ImportDate { get; set; } = DateTime.UtcNow;
        public int VersionNumber { get; set; } = 1;
        public string? ImportedBy { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? FileHash { get; set; }              // MD5 hash for duplicate detection

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    }
}
