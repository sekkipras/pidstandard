namespace PIDStandardization.Services.TaggingServices
{
    /// <summary>
    /// Interface for tag validation services
    /// Handles both Custom and KKS tag validation
    /// </summary>
    public interface ITagValidationService
    {
        /// <summary>
        /// Validates a tag number based on project's tagging mode
        /// </summary>
        /// <param name="excludeEquipmentId">Optional equipment ID to exclude from uniqueness check (for edit mode)</param>
        Task<TagValidationResult> ValidateTagAsync(Guid projectId, string tagNumber, Guid? excludeEquipmentId = null);

        /// <summary>
        /// Checks if a tag is unique within the project
        /// </summary>
        Task<bool> IsTagUniqueAsync(Guid projectId, string tagNumber, Guid? excludeEquipmentId = null);

        /// <summary>
        /// Generates the next available tag number based on project rules
        /// </summary>
        Task<string> GenerateNextTagAsync(Guid projectId, string equipmentType, string? area = null);
    }

    /// <summary>
    /// Result of tag validation
    /// </summary>
    public class TagValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsUnique { get; set; }
        public bool FormatCompliant { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
