using PIDStandardization.Core.Enums;
using PIDStandardization.Core.Interfaces;

namespace PIDStandardization.Services.TaggingServices
{
    /// <summary>
    /// Service for validating equipment tags
    /// </summary>
    public class TagValidationService : ITagValidationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TagValidationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TagValidationResult> ValidateTagAsync(Guid projectId, string tagNumber)
        {
            var result = new TagValidationResult();

            if (string.IsNullOrWhiteSpace(tagNumber))
            {
                result.IsValid = false;
                result.Errors.Add("Tag number cannot be empty");
                return result;
            }

            // Get project to determine tagging mode
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
            {
                result.IsValid = false;
                result.Errors.Add("Project not found");
                return result;
            }

            // Validate format based on tagging mode
            if (project.TaggingMode == TaggingMode.Custom)
            {
                result.FormatCompliant = ValidateCustomFormat(tagNumber, result);
            }
            else if (project.TaggingMode == TaggingMode.KKS)
            {
                result.FormatCompliant = ValidateKKSFormat(tagNumber, result);
            }

            // Check uniqueness
            result.IsUnique = await IsTagUniqueAsync(projectId, tagNumber);
            if (!result.IsUnique)
            {
                result.Errors.Add($"Tag '{tagNumber}' already exists in this project");
            }

            result.IsValid = result.FormatCompliant && result.IsUnique && result.Errors.Count == 0;
            return result;
        }

        public async Task<bool> IsTagUniqueAsync(Guid projectId, string tagNumber, Guid? excludeEquipmentId = null)
        {
            var existingEquipment = await _unitOfWork.Equipment.FindAsync(
                e => e.ProjectId == projectId && e.TagNumber == tagNumber);

            if (excludeEquipmentId.HasValue)
            {
                existingEquipment = existingEquipment.Where(e => e.EquipmentId != excludeEquipmentId.Value);
            }

            return !existingEquipment.Any();
        }

        public async Task<string> GenerateNextTagAsync(Guid projectId, string equipmentType, string? area = null)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
            {
                throw new InvalidOperationException("Project not found");
            }

            // Get maximum sequence number for this prefix (optimized - doesn't load all equipment)
            // Build prefix based on tagging mode
            string prefix;
            if (project.TaggingMode == TaggingMode.Custom)
            {
                prefix = $"P-{area ?? "000"}-{equipmentType}";
            }
            else
            {
                prefix = equipmentType;
            }

            var maxNumber = await _unitOfWork.Equipment.GetMaxSequenceNumberAsync(projectId, prefix);
            var nextNumber = maxNumber + 1;

            // Generate based on tagging mode
            if (project.TaggingMode == TaggingMode.Custom)
            {
                return $"P-{area ?? "000"}-{equipmentType}-{nextNumber:D3}";
            }
            else
            {
                return $"+LAA 10 {equipmentType}{nextNumber:D3}";
            }
        }

        private bool ValidateCustomFormat(string tagNumber, TagValidationResult result)
        {
            // Basic custom format validation
            // Format: [Prefix]-[Area]-[Type]-[Sequence]
            // Example: P-100-PMP-001

            if (tagNumber.Length < 3 || tagNumber.Length > 50)
            {
                result.Errors.Add("Tag length must be between 3 and 50 characters");
                return false;
            }

            if (tagNumber.Contains(' '))
            {
                result.Errors.Add("Custom tags should not contain spaces");
                return false;
            }

            return true;
        }

        private bool ValidateKKSFormat(string tagNumber, TagValidationResult result)
        {
            // KKS format validation
            // Format: +[ABC] [DD] [EE][NNN]
            // Example: +LAA 10 CP001

            if (!tagNumber.StartsWith("+"))
            {
                result.Errors.Add("KKS tag must start with '+'");
                return false;
            }

            var parts = tagNumber.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                result.Errors.Add("KKS tag must have format: +[Function] [Location] [Equipment]");
                return false;
            }

            // Validate function key (3 characters including +)
            if (parts[0].Length < 3 || parts[0].Length > 4)
            {
                result.Errors.Add("KKS function key must be 2-3 characters after '+'");
                return false;
            }

            // Validate location key (2 digits)
            if (!int.TryParse(parts[1], out _) || parts[1].Length != 2)
            {
                result.Errors.Add("KKS location key must be 2 digits");
                return false;
            }

            // Validate equipment identifier (2 letters + 3 digits)
            if (parts[2].Length < 5)
            {
                result.Errors.Add("KKS equipment identifier must be at least 5 characters");
                return false;
            }

            return true;
        }

        private int? ExtractSequenceNumber(string tagNumber)
        {
            // Try to extract the last numeric sequence from the tag
            var parts = tagNumber.Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var lastPart = parts.LastOrDefault();

            if (lastPart != null && int.TryParse(new string(lastPart.Where(char.IsDigit).ToArray()), out int number))
            {
                return number;
            }

            return null;
        }
    }
}
