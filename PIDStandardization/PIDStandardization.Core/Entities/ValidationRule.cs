using PIDStandardization.Core.Enums;

namespace PIDStandardization.Core.Entities
{
    /// <summary>
    /// Represents a validation rule for design rule checking
    /// </summary>
    public class ValidationRule
    {
        public Guid RuleId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string? Category { get; set; }

        public ValidationSeverity Severity { get; set; }
        public string? Description { get; set; }
        public string? ParametersJson { get; set; }

        public bool IsEnabled { get; set; } = true;
        public bool IsCustom { get; set; } = false;
    }
}
