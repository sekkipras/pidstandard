using FluentAssertions;
using PIDStandardization.Core.Configuration;
using Xunit;

namespace PIDStandardization.Tests
{
    public class ConfigurationServiceTests
    {
        [Theory]
        [InlineData("PUMP-001", "Pump")]
        [InlineData("PMP-A-001", "Pump")]
        [InlineData("P-100-001", "Pump")]
        [InlineData("VALVE-001", "Valve")]
        [InlineData("VLV-001", "Valve")]
        [InlineData("V-100-001", "Valve")]
        [InlineData("TANK-001", "Tank")]
        [InlineData("TK-100", "Tank")]
        [InlineData("T-100-001", "Tank")]
        [InlineData("VESSEL-001", "Vessel")]
        [InlineData("VS-100", "Vessel")]
        [InlineData("HX-001", "Heat Exchanger")]
        [InlineData("HEAT_EXCHANGER-001", "Heat Exchanger")]
        [InlineData("FILTER-001", "Filter")]
        [InlineData("FLT-001", "Filter")]
        [InlineData("COMPRESSOR-001", "Compressor")]
        [InlineData("COMP-001", "Compressor")]
        [InlineData("SEPARATOR-001", "Separator")]
        [InlineData("SEP-001", "Separator")]
        [InlineData("INSTRUMENT-001", "Instrument")]
        [InlineData("INST-001", "Instrument")]
        public void GetEquipmentType_ShouldReturnCorrectType_ForKnownPatterns(string blockName, string expectedType)
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.GetEquipmentType(blockName);

            // Assert
            result.Should().Be(expectedType);
        }

        [Theory]
        [InlineData("UNKNOWN_BLOCK", "Equipment")]
        [InlineData("XYZ-001", "Equipment")]
        [InlineData("CUSTOM-BLOCK", "Equipment")]
        [InlineData("", "Equipment")]
        public void GetEquipmentType_ShouldReturnDefaultType_ForUnknownPatterns(string blockName, string expectedType)
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.GetEquipmentType(blockName);

            // Assert
            result.Should().Be(expectedType);
        }

        [Fact]
        public void GetEquipmentType_ShouldBeCaseInsensitive()
        {
            // Arrange
            var service = new ConfigurationService();

            // Act & Assert
            service.GetEquipmentType("pump-001").Should().Be("Pump");
            service.GetEquipmentType("PUMP-001").Should().Be("Pump");
            service.GetEquipmentType("Pump-001").Should().Be("Pump");
        }

        [Theory]
        [InlineData("Pump", "P")]
        [InlineData("Valve", "V")]
        [InlineData("Tank", "TK")]
        [InlineData("Vessel", "VS")]
        [InlineData("Heat Exchanger", "HX")]
        [InlineData("Filter", "F")]
        [InlineData("Compressor", "C")]
        [InlineData("Separator", "S")]
        public void GetEquipmentCode_ShouldReturnCorrectCode(string equipmentType, string expectedCode)
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.GetEquipmentCode(equipmentType);

            // Assert
            result.Should().Be(expectedCode);
        }

        [Fact]
        public void GetEquipmentCode_ShouldReturnFirstTwoChars_ForUnknownType()
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.GetEquipmentCode("CustomEquipment");

            // Assert
            result.Should().Be("CU");
        }

        [Fact]
        public void Settings_ShouldHaveDefaultValues()
        {
            // Arrange
            var service = new ConfigurationService();

            // Assert
            service.Settings.Should().NotBeNull();
            service.Settings.DatabaseSettings.Should().NotBeNull();
            service.Settings.EquipmentTypes.Should().NotBeNull();
            service.Settings.Tagging.Should().NotBeNull();
            service.Settings.Paths.Should().NotBeNull();
            service.Settings.Logging.Should().NotBeNull();
        }

        [Fact]
        public void Settings_EquipmentTypes_ShouldHaveDefaultTypes()
        {
            // Arrange
            var service = new ConfigurationService();

            // Assert
            service.Settings.EquipmentTypes.Types.Should().NotBeEmpty();
            service.Settings.EquipmentTypes.Types.Should().Contain(t => t.Name == "Pump");
            service.Settings.EquipmentTypes.Types.Should().Contain(t => t.Name == "Valve");
            service.Settings.EquipmentTypes.Types.Should().Contain(t => t.Name == "Tank");
        }

        [Fact]
        public void Settings_Tagging_ShouldHaveDefaultAttributeNames()
        {
            // Arrange
            var service = new ConfigurationService();

            // Assert
            service.Settings.Tagging.TagAttributeNames.Should().NotBeEmpty();
            service.Settings.Tagging.TagAttributeNames.Should().Contain("TAG");
            service.Settings.Tagging.TagAttributeNames.Should().Contain("TAGNUMBER");
        }

        [Fact]
        public void ResolvePath_ShouldExpandEnvironmentVariables()
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.ResolvePath("{CommonApplicationData}\\Test");

            // Assert
            result.Should().NotContain("{CommonApplicationData}");
            result.Should().EndWith("Test");
        }

        [Fact]
        public void ResolvePath_ShouldHandleEmptyString()
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.ResolvePath("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ResolvePath_ShouldHandleNullString()
        {
            // Arrange
            var service = new ConfigurationService();

            // Act
            var result = service.ResolvePath(null!);

            // Assert
            result.Should().BeNull();
        }
    }
}
