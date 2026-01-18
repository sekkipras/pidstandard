using Microsoft.Data.SqlClient;
using System.IO;

namespace PIDStandardization.UI.Helpers
{
    /// <summary>
    /// Helper class to translate technical exceptions into user-friendly error messages
    /// </summary>
    public static class UserErrorMessages
    {
        /// <summary>
        /// Get user-friendly database error message
        /// </summary>
        public static string GetDatabaseError(SqlException ex)
        {
            return ex.Number switch
            {
                -2 => "Database connection timed out.\n\n" +
                      "Please check:\n" +
                      "• SQL Server is running\n" +
                      "• Network connection is available\n" +
                      "• Firewall allows database access\n\n" +
                      "Contact your administrator if problem persists.",

                4060 => "Database not found.\n\n" +
                        "The PIDStandardization database does not exist on the server.\n\n" +
                        "Please run the database migration first:\n" +
                        "See MIGRATION_README.md for instructions.",

                18456 => "Login failed - invalid credentials.\n\n" +
                         "Cannot connect to database with the provided username/password.\n\n" +
                         "Please check your database connection settings in appsettings.json.",

                2627 or 2601 => "Duplicate entry detected.\n\n" +
                               "This record already exists in the database.\n" +
                               "Please check the tag number or identifier and try again.",

                547 => "Cannot delete this record.\n\n" +
                       "Other records are using this item.\n" +
                       "Please remove related records first or contact your administrator.",

                _ => $"Database error occurred (Error Code: {ex.Number}).\n\n" +
                     $"Please check the log file for details.\n" +
                     $"Contact your administrator for assistance.\n\n" +
                     $"Technical details: {ex.Message}"
            };
        }

        /// <summary>
        /// Get user-friendly file access error message
        /// </summary>
        public static string GetFileAccessError(IOException ex)
        {
            if (ex.Message.Contains("being used by another process"))
            {
                return "File is currently open in another application.\n\n" +
                       "Please close the file in AutoCAD or Excel and try again.";
            }

            if (ex.Message.Contains("Access") && ex.Message.Contains("denied"))
            {
                return "Access denied to file or folder.\n\n" +
                       "Please check that you have permission to access this location.\n" +
                       "Contact your administrator if problem persists.";
            }

            if (ex.Message.Contains("not find") || ex.Message.Contains("does not exist"))
            {
                return "File or folder not found.\n\n" +
                       "Please check the file path and try again.";
            }

            return $"File access error occurred.\n\n" +
                   $"Please check:\n" +
                   $"• File path is correct\n" +
                   $"• File is not locked by another user\n" +
                   $"• You have permission to access the file\n\n" +
                   $"Technical details: {ex.Message}";
        }

        /// <summary>
        /// Get user-friendly duplicate tag error message
        /// </summary>
        public static string GetDuplicateTagError(string tagNumber)
        {
            return $"Tag number already exists: {tagNumber}\n\n" +
                   $"This tag is already used by another equipment item in this project.\n\n" +
                   $"Please choose a different tag number or check for duplicates.";
        }

        /// <summary>
        /// Get user-friendly validation error message
        /// </summary>
        public static string GetValidationError(string fieldName, string issue)
        {
            return $"Validation Error: {fieldName}\n\n" +
                   $"{issue}\n\n" +
                   $"Please correct the value and try again.";
        }

        /// <summary>
        /// Get user-friendly import error message
        /// </summary>
        public static string GetImportError(string fileName, Exception ex)
        {
            if (ex is IOException)
            {
                return GetFileAccessError((IOException)ex);
            }

            if (ex.Message.Contains("not a valid Excel file") || ex.Message.Contains("Workbook"))
            {
                return $"Invalid Excel file format.\n\n" +
                       $"File: {Path.GetFileName(fileName)}\n\n" +
                       $"Please ensure:\n" +
                       $"• File is in Excel format (.xlsx)\n" +
                       $"• File is not corrupted\n" +
                       $"• Sheet names match expected format";
            }

            return $"Error importing file: {Path.GetFileName(fileName)}\n\n" +
                   $"{ex.Message}\n\n" +
                   $"Please check the file format and try again.";
        }

        /// <summary>
        /// Get user-friendly configuration error message
        /// </summary>
        public static string GetConfigurationError(Exception ex)
        {
            return "Configuration error occurred.\n\n" +
                   "Cannot load application settings.\n\n" +
                   "Please check:\n" +
                   "• appsettings.json exists\n" +
                   "• JSON format is valid\n" +
                   "• Connection string is correct\n\n" +
                   $"Technical details: {ex.Message}";
        }

        /// <summary>
        /// Format exception for logging with correlation ID
        /// </summary>
        public static (string UserMessage, string LogMessage, Guid CorrelationId) FormatException(
            Exception ex,
            string operation)
        {
            var correlationId = Guid.NewGuid();

            var userMessage = ex switch
            {
                SqlException sqlEx => GetDatabaseError(sqlEx),
                IOException ioEx => GetFileAccessError(ioEx),
                InvalidOperationException invalidOp when invalidOp.Message.Contains("tag") =>
                    GetDuplicateTagError("Unknown"),
                _ => $"An error occurred during {operation}.\n\n" +
                     $"Error ID: {correlationId}\n\n" +
                     $"Please check the log file or contact support with this Error ID."
            };

            var logMessage = $"[{correlationId}] Error in {operation}: {ex.GetType().Name} - {ex.Message}\n" +
                           $"Stack Trace: {ex.StackTrace}";

            if (ex.InnerException != null)
            {
                logMessage += $"\nInner Exception: {ex.InnerException.Message}";
            }

            return (userMessage, logMessage, correlationId);
        }
    }
}
