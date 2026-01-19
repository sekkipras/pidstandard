using FluentAssertions;
using PIDStandardization.UI.Helpers;
using System.IO;
using Xunit;

namespace PIDStandardization.Tests
{
    public class UserErrorMessagesTests
    {
        [Fact]
        public void GetFileAccessError_ShouldReturnCorrectMessage_ForFileNotFoundException()
        {
            // Arrange
            var exception = new FileNotFoundException("File not found", "test.xlsx");

            // Act
            var result = UserErrorMessages.GetFileAccessError(exception);

            // Assert
            result.Should().Contain("could not be found");
            result.Should().Contain("file exists");
        }

        [Fact]
        public void GetFileAccessError_ShouldReturnCorrectMessage_ForDirectoryNotFoundException()
        {
            // Arrange
            var exception = new DirectoryNotFoundException("Directory not found");

            // Act
            var result = UserErrorMessages.GetFileAccessError(exception);

            // Assert
            result.Should().Contain("folder does not exist");
        }

        [Fact]
        public void GetFileAccessError_ShouldReturnCorrectMessage_ForUnauthorizedAccessException()
        {
            // Arrange
            var exception = new UnauthorizedAccessException("Access denied");

            // Act
            var result = UserErrorMessages.GetFileAccessError(exception);

            // Assert
            result.Should().Contain("Permission denied");
            result.Should().Contain("administrator");
        }

        [Fact]
        public void GetFileAccessError_ShouldReturnCorrectMessage_ForIOException()
        {
            // Arrange
            var exception = new IOException("The process cannot access the file because it is being used by another process.");

            // Act
            var result = UserErrorMessages.GetFileAccessError(exception);

            // Assert
            result.Should().Contain("open in another application");
        }

        [Fact]
        public void GetDuplicateTagError_ShouldIncludeTagNumber()
        {
            // Arrange
            var tagNumber = "P-100-001";

            // Act
            var result = UserErrorMessages.GetDuplicateTagError(tagNumber);

            // Assert
            result.Should().Contain(tagNumber);
            result.Should().Contain("already exists");
        }

        [Fact]
        public void GetValidationError_ShouldIncludeFieldName()
        {
            // Arrange
            var fieldName = "TagNumber";
            var message = "Cannot be empty";

            // Act
            var result = UserErrorMessages.GetValidationError(fieldName, message);

            // Assert
            result.Should().Contain(fieldName);
            result.Should().Contain(message);
        }

        [Fact]
        public void GetImportError_ShouldIncludeFileName()
        {
            // Arrange
            var fileName = "equipment.xlsx";
            var exception = new Exception("Invalid format");

            // Act
            var result = UserErrorMessages.GetImportError(fileName, exception);

            // Assert
            result.Should().Contain(fileName);
        }

        [Fact]
        public void FormatException_ShouldReturnUserMessageAndLogMessage()
        {
            // Arrange
            var exception = new Exception("Test error");
            var operation = "loading data";

            // Act
            var (userMessage, logMessage, correlationId) = UserErrorMessages.FormatException(exception, operation);

            // Assert
            userMessage.Should().NotBeEmpty();
            userMessage.Should().Contain(correlationId.ToString());
            logMessage.Should().Contain("Test error");
            logMessage.Should().Contain(correlationId.ToString());
            correlationId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void FormatException_ShouldGenerateUniqueCorrelationIds()
        {
            // Arrange
            var exception = new Exception("Test");

            // Act
            var (_, _, correlationId1) = UserErrorMessages.FormatException(exception, "op1");
            var (_, _, correlationId2) = UserErrorMessages.FormatException(exception, "op2");

            // Assert
            correlationId1.Should().NotBe(correlationId2);
        }

        [Fact]
        public void GetConfigurationError_ShouldReturnCorrectMessage()
        {
            // Arrange
            var settingName = "ConnectionString";

            // Act
            var result = UserErrorMessages.GetConfigurationError(settingName);

            // Assert
            result.Should().Contain(settingName);
            result.Should().Contain("configuration");
        }
    }
}
