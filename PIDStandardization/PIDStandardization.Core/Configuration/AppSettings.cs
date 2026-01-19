namespace PIDStandardization.Core.Configuration
{
    /// <summary>
    /// Root application settings loaded from appsettings.json
    /// </summary>
    public class AppSettings
    {
        public DatabaseSettings DatabaseSettings { get; set; } = new();
        public EquipmentTypeSettings EquipmentTypes { get; set; } = new();
        public TaggingSettings Tagging { get; set; } = new();
        public PathSettings Paths { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    /// <summary>
    /// Database connection settings
    /// </summary>
    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = "Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;";
        public bool EnableSensitiveDataLogging { get; set; } = false;
        public int CommandTimeout { get; set; } = 30;
        public int MaxRetryCount { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;
    }

    /// <summary>
    /// Equipment type detection patterns
    /// </summary>
    public class EquipmentTypeSettings
    {
        /// <summary>
        /// List of equipment types with their detection patterns
        /// </summary>
        public List<EquipmentTypeDefinition> Types { get; set; } = new()
        {
            new EquipmentTypeDefinition { Name = "Pump", Code = "P", Patterns = new[] { "PUMP", "PMP", "P-" } },
            new EquipmentTypeDefinition { Name = "Valve", Code = "V", Patterns = new[] { "VALVE", "VLV", "V-" } },
            new EquipmentTypeDefinition { Name = "Tank", Code = "TK", Patterns = new[] { "TANK", "TK", "T-" } },
            new EquipmentTypeDefinition { Name = "Vessel", Code = "VS", Patterns = new[] { "VESSEL", "VSL", "VS" } },
            new EquipmentTypeDefinition { Name = "Heat Exchanger", Code = "HX", Patterns = new[] { "HX", "HEAT", "EXCHANGER" } },
            new EquipmentTypeDefinition { Name = "Filter", Code = "F", Patterns = new[] { "FILTER", "FLT", "F-" } },
            new EquipmentTypeDefinition { Name = "Compressor", Code = "C", Patterns = new[] { "COMPRESSOR", "COMP", "C-" } },
            new EquipmentTypeDefinition { Name = "Separator", Code = "S", Patterns = new[] { "SEPARATOR", "SEP", "S-" } },
            new EquipmentTypeDefinition { Name = "Reactor", Code = "R", Patterns = new[] { "REACTOR", "RX", "R-" } },
            new EquipmentTypeDefinition { Name = "Instrument", Code = "I", Patterns = new[] { "INST", "INSTRUMENT" } }
        };

        /// <summary>
        /// Default type when no pattern matches
        /// </summary>
        public string DefaultType { get; set; } = "Equipment";
    }

    /// <summary>
    /// Definition of a single equipment type with detection patterns
    /// </summary>
    public class EquipmentTypeDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string[] Patterns { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Tagging format settings
    /// </summary>
    public class TaggingSettings
    {
        /// <summary>
        /// Format for Custom tagging mode: {EquipmentType}-{Area}-{Sequence}
        /// </summary>
        public string CustomTagFormat { get; set; } = "P-{Area}-{Type}{Sequence:D3}";

        /// <summary>
        /// Format for KKS tagging mode
        /// </summary>
        public string KksTagFormat { get; set; } = "{SystemCode}{EquipmentCode}{CounterNumber}";

        /// <summary>
        /// Default sequence number padding (number of digits)
        /// </summary>
        public int SequenceNumberPadding { get; set; } = 3;

        /// <summary>
        /// Starting sequence number for new tags
        /// </summary>
        public int StartingSequenceNumber { get; set; } = 1;

        /// <summary>
        /// Tag attribute names to search for in AutoCAD blocks
        /// </summary>
        public string[] TagAttributeNames { get; set; } = { "TAG", "TAGNUMBER", "TAG_NUMBER", "TAGNO", "EQUIPMENT_TAG" };
    }

    /// <summary>
    /// File and directory path settings
    /// </summary>
    public class PathSettings
    {
        /// <summary>
        /// Base path for application data storage
        /// </summary>
        public string AppDataPath { get; set; } = "{CommonApplicationData}\\PIDStandardization";

        /// <summary>
        /// Path for storing drawings
        /// </summary>
        public string DrawingsPath { get; set; } = "{AppDataPath}\\Drawings";

        /// <summary>
        /// Path for log files
        /// </summary>
        public string LogsPath { get; set; } = "{AppDataPath}\\Logs";

        /// <summary>
        /// Path for export files
        /// </summary>
        public string ExportsPath { get; set; } = "{AppDataPath}\\Exports";

        /// <summary>
        /// Path for temporary files
        /// </summary>
        public string TempPath { get; set; } = "{AppDataPath}\\Temp";
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingSettings
    {
        public string MinimumLevel { get; set; } = "Debug";
        public int RetainedFileCountLimit { get; set; } = 30;
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
    }
}
