using PIDStandardization.Core.Enums;
using System.Text.RegularExpressions;

namespace PIDStandardization.AutoCAD.Services
{
    /// <summary>
    /// Service for validating tag numbers based on project tagging mode
    /// </summary>
    public class TagValidationService
    {
        /// <summary>
        /// Validates a tag number based on the project's tagging mode
        /// </summary>
        /// <param name="tagNumber">The tag number to validate</param>
        /// <param name="taggingMode">The project's tagging mode</param>
        /// <returns>ValidationResult containing success status and any error messages</returns>
        public ValidationResult ValidateTag(string tagNumber, TaggingMode taggingMode)
        {
            if (string.IsNullOrWhiteSpace(tagNumber))
            {
                return new ValidationResult(false, "Tag number cannot be empty.");
            }

            // Remove leading/trailing whitespace
            tagNumber = tagNumber.Trim();

            // Check for invalid characters
            if (tagNumber.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.'))
            {
                return new ValidationResult(false, "Tag number contains invalid characters. Only letters, numbers, hyphens, underscores, and periods are allowed.");
            }

            switch (taggingMode)
            {
                case TaggingMode.Custom:
                    return ValidateCustomTag(tagNumber);

                case TaggingMode.KKS:
                    return ValidateKKSTag(tagNumber);

                default:
                    return new ValidationResult(true, string.Empty);
            }
        }

        /// <summary>
        /// Validates a custom tag number
        /// Custom mode: Flexible format, typically PREFIX-NUMBER (e.g., PUMP-001, TK-100)
        /// </summary>
        private ValidationResult ValidateCustomTag(string tagNumber)
        {
            // Custom tags should follow a general pattern: PREFIX-NUMBER or PREFIX_NUMBER
            // Examples: P-101, PUMP-001, TK_200, HX-301A

            // Minimum length check
            if (tagNumber.Length < 3)
            {
                return new ValidationResult(false, "Custom tag number is too short. Minimum 3 characters required.");
            }

            // Should contain at least one letter
            if (!tagNumber.Any(char.IsLetter))
            {
                return new ValidationResult(false, "Custom tag should contain at least one letter.");
            }

            // Should contain at least one digit or separator
            if (!tagNumber.Any(char.IsDigit) && !tagNumber.Contains('-') && !tagNumber.Contains('_'))
            {
                return new ValidationResult(false, "Custom tag should follow format: PREFIX-NUMBER (e.g., P-101, PUMP-001).");
            }

            return new ValidationResult(true, string.Empty);
        }

        /// <summary>
        /// Validates a KKS (Kraftwerk-Kennzeichensystem) tag number
        /// KKS format: DIN 40719 standard
        /// Format: +AAA-BBB-CCC (e.g., +10P-AA101-M01)
        /// - Function key (optional): + or =
        /// - Plant section: 2-3 alphanumeric
        /// - Process/Function: 2-3 alphanumeric
        /// - Component: 2-4 alphanumeric
        /// </summary>
        private ValidationResult ValidateKKSTag(string tagNumber)
        {
            // KKS standard format validation
            // Basic structure: [+|-|=]XXX[-]YYY[-]ZZZ
            // Examples: +10P-AA101-M01, 10P-AA101, =GAA-BB001-C1

            // Check for valid KKS prefix (optional)
            string pattern = @"^[+=]?[A-Z0-9]{2,3}[-][A-Z0-9]{2,5}(?:[-][A-Z0-9]{1,4})?$";

            if (!Regex.IsMatch(tagNumber.ToUpper(), pattern))
            {
                return new ValidationResult(
                    false,
                    "KKS tag format invalid. Expected: [+]AAA-BBB[-CCC]\n" +
                    "Examples: +10P-AA101-M01, 20P-BB201, =GAA-001-C1\n" +
                    "- Optional prefix: +, =\n" +
                    "- Plant section: 2-3 alphanumeric\n" +
                    "- Process: 2-5 alphanumeric\n" +
                    "- Component (optional): 1-4 alphanumeric"
                );
            }

            // Additional KKS validation rules
            var parts = tagNumber.TrimStart('+', '=').Split('-');

            if (parts.Length < 2)
            {
                return new ValidationResult(false, "KKS tag must have at least two parts separated by hyphen (AAA-BBB).");
            }

            if (parts.Length > 3)
            {
                return new ValidationResult(false, "KKS tag can have maximum three parts (AAA-BBB-CCC).");
            }

            // Validate plant section (first part)
            if (parts[0].Length < 2 || parts[0].Length > 3)
            {
                return new ValidationResult(false, "KKS plant section must be 2-3 characters.");
            }

            // Validate process/function (second part)
            if (parts[1].Length < 2 || parts[1].Length > 5)
            {
                return new ValidationResult(false, "KKS process/function must be 2-5 characters.");
            }

            // Validate component (third part, if present)
            if (parts.Length == 3 && (parts[2].Length < 1 || parts[2].Length > 4))
            {
                return new ValidationResult(false, "KKS component identifier must be 1-4 characters.");
            }

            return new ValidationResult(true, string.Empty);
        }

        /// <summary>
        /// Suggests corrections for common tag formatting issues
        /// </summary>
        public string SuggestCorrection(string tagNumber, TaggingMode taggingMode)
        {
            if (string.IsNullOrWhiteSpace(tagNumber))
                return string.Empty;

            // Convert to uppercase (standard practice)
            string corrected = tagNumber.Trim().ToUpper();

            if (taggingMode == TaggingMode.KKS)
            {
                // Replace underscores with hyphens for KKS
                corrected = corrected.Replace('_', '-');

                // Remove spaces
                corrected = corrected.Replace(" ", string.Empty);
            }
            else if (taggingMode == TaggingMode.Custom)
            {
                // Remove spaces
                corrected = corrected.Replace(" ", string.Empty);

                // If no separator exists but has letters and numbers, suggest adding hyphen
                if (!corrected.Contains('-') && !corrected.Contains('_'))
                {
                    // Find transition from letters to numbers
                    for (int i = 0; i < corrected.Length - 1; i++)
                    {
                        if (char.IsLetter(corrected[i]) && char.IsDigit(corrected[i + 1]))
                        {
                            corrected = corrected.Insert(i + 1, "-");
                            break;
                        }
                    }
                }
            }

            return corrected;
        }

        /// <summary>
        /// Gets a description of the expected format for the given tagging mode
        /// </summary>
        public string GetFormatDescription(TaggingMode taggingMode)
        {
            switch (taggingMode)
            {
                case TaggingMode.Custom:
                    return "Custom Format: PREFIX-NUMBER\n" +
                           "Examples: P-101, PUMP-001, TK-200A, HX-301\n" +
                           "Requirements:\n" +
                           "- Minimum 3 characters\n" +
                           "- Must contain letters and numbers\n" +
                           "- Use hyphens or underscores as separators\n" +
                           "- Only alphanumeric, hyphens, underscores, periods allowed";

                case TaggingMode.KKS:
                    return "KKS Format (DIN 40719): [+]AAA-BBB[-CCC]\n" +
                           "Examples: +10P-AA101-M01, 20P-BB201, =GAA-001-C1\n" +
                           "Requirements:\n" +
                           "- Optional prefix: + or =\n" +
                           "- Plant section: 2-3 alphanumeric characters\n" +
                           "- Process/Function: 2-5 alphanumeric characters\n" +
                           "- Component (optional): 1-4 alphanumeric characters\n" +
                           "- Parts separated by hyphens";

                default:
                    return "No specific format requirements.";
            }
        }
    }

    /// <summary>
    /// Represents the result of a tag validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }
}
