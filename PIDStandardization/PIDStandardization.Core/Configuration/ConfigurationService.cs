using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;

namespace PIDStandardization.Core.Configuration
{
    /// <summary>
    /// Interface for configuration service to support DI
    /// </summary>
    public interface IConfigurationService
    {
        AppSettings Settings { get; }
        string GetEquipmentType(string blockName);
        string GetEquipmentCode(string equipmentType);
        string ResolvePath(string pathTemplate);
        void ReloadConfiguration();
    }

    /// <summary>
    /// Centralized configuration service for loading and managing application settings
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private AppSettings _settings;
        private readonly string _configFilePath;
        private static ConfigurationService? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static ConfigurationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ConfigurationService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the current application settings
        /// </summary>
        public AppSettings Settings => _settings;

        /// <summary>
        /// Constructor - loads configuration from default locations
        /// </summary>
        public ConfigurationService()
        {
            _settings = new AppSettings();
            _configFilePath = FindConfigurationFile();
            LoadConfiguration();
        }

        /// <summary>
        /// Constructor with explicit config file path (for testing)
        /// </summary>
        public ConfigurationService(string configFilePath)
        {
            _settings = new AppSettings();
            _configFilePath = configFilePath;
            LoadConfiguration();
        }

        private string FindConfigurationFile()
        {
            // Search in multiple locations
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "PIDStandardization", "appsettings.json"),
                Path.Combine(Environment.CurrentDirectory, "appsettings.json")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    Log.Debug("Configuration file found at: {Path}", path);
                    return path;
                }
            }

            Log.Warning("No configuration file found. Using default settings.");
            return searchPaths[0]; // Return default path even if file doesn't exist
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Path.GetDirectoryName(_configFilePath) ?? "")
                        .AddJsonFile(Path.GetFileName(_configFilePath), optional: true, reloadOnChange: false);

                    var configuration = builder.Build();

                    // Bind database settings
                    var dbSection = configuration.GetSection("DatabaseSettings");
                    if (dbSection.Exists())
                    {
                        _settings.DatabaseSettings = new DatabaseSettings
                        {
                            ConnectionString = dbSection["ConnectionString"] ?? _settings.DatabaseSettings.ConnectionString,
                            EnableSensitiveDataLogging = bool.TryParse(dbSection["EnableSensitiveDataLogging"], out var sensitive) && sensitive,
                            CommandTimeout = int.TryParse(dbSection["CommandTimeout"], out var timeout) ? timeout : 30,
                            MaxRetryCount = int.TryParse(dbSection["MaxRetryCount"], out var retry) ? retry : 3,
                            RetryDelayMilliseconds = int.TryParse(dbSection["RetryDelayMilliseconds"], out var delay) ? delay : 1000
                        };
                    }

                    // Bind equipment types
                    var equipmentSection = configuration.GetSection("EquipmentTypes");
                    if (equipmentSection.Exists())
                    {
                        var types = equipmentSection.GetSection("Types").GetChildren();
                        if (types.Any())
                        {
                            _settings.EquipmentTypes.Types = types.Select(t => new EquipmentTypeDefinition
                            {
                                Name = t["Name"] ?? "",
                                Code = t["Code"] ?? "",
                                Patterns = t.GetSection("Patterns").GetChildren().Select(p => p.Value ?? "").ToArray()
                            }).ToList();
                        }

                        var defaultType = equipmentSection["DefaultType"];
                        if (!string.IsNullOrEmpty(defaultType))
                        {
                            _settings.EquipmentTypes.DefaultType = defaultType;
                        }
                    }

                    // Bind tagging settings
                    var taggingSection = configuration.GetSection("Tagging");
                    if (taggingSection.Exists())
                    {
                        _settings.Tagging = new TaggingSettings
                        {
                            CustomTagFormat = taggingSection["CustomTagFormat"] ?? _settings.Tagging.CustomTagFormat,
                            KksTagFormat = taggingSection["KksTagFormat"] ?? _settings.Tagging.KksTagFormat,
                            SequenceNumberPadding = int.TryParse(taggingSection["SequenceNumberPadding"], out var padding) ? padding : 3,
                            StartingSequenceNumber = int.TryParse(taggingSection["StartingSequenceNumber"], out var start) ? start : 1
                        };

                        var tagAttrs = taggingSection.GetSection("TagAttributeNames").GetChildren().Select(p => p.Value ?? "").ToArray();
                        if (tagAttrs.Any())
                        {
                            _settings.Tagging.TagAttributeNames = tagAttrs;
                        }
                    }

                    // Bind path settings
                    var pathsSection = configuration.GetSection("Paths");
                    if (pathsSection.Exists())
                    {
                        _settings.Paths = new PathSettings
                        {
                            AppDataPath = pathsSection["AppDataPath"] ?? _settings.Paths.AppDataPath,
                            DrawingsPath = pathsSection["DrawingsPath"] ?? _settings.Paths.DrawingsPath,
                            LogsPath = pathsSection["LogsPath"] ?? _settings.Paths.LogsPath,
                            ExportsPath = pathsSection["ExportsPath"] ?? _settings.Paths.ExportsPath,
                            TempPath = pathsSection["TempPath"] ?? _settings.Paths.TempPath
                        };
                    }

                    // Bind logging settings
                    var loggingSection = configuration.GetSection("Logging");
                    if (loggingSection.Exists())
                    {
                        _settings.Logging = new LoggingSettings
                        {
                            MinimumLevel = loggingSection["MinimumLevel"] ?? _settings.Logging.MinimumLevel,
                            RetainedFileCountLimit = int.TryParse(loggingSection["RetainedFileCountLimit"], out var limit) ? limit : 30,
                            OutputTemplate = loggingSection["OutputTemplate"] ?? _settings.Logging.OutputTemplate,
                            EnableConsoleLogging = !bool.TryParse(loggingSection["EnableConsoleLogging"], out var console) || console,
                            EnableFileLogging = !bool.TryParse(loggingSection["EnableFileLogging"], out var file) || file
                        };
                    }

                    Log.Information("Configuration loaded from {Path}", _configFilePath);
                }
                else
                {
                    Log.Warning("Configuration file not found at {Path}. Using default settings.", _configFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading configuration from {Path}. Using default settings.", _configFilePath);
            }
        }

        /// <summary>
        /// Reloads configuration from file
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadConfiguration();
            Log.Information("Configuration reloaded");
        }

        /// <summary>
        /// Gets equipment type from block name using configured patterns
        /// </summary>
        public string GetEquipmentType(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return _settings.EquipmentTypes.DefaultType;

            string upperName = blockName.ToUpper();

            foreach (var typeDefinition in _settings.EquipmentTypes.Types)
            {
                foreach (var pattern in typeDefinition.Patterns)
                {
                    if (upperName.Contains(pattern.ToUpper()))
                    {
                        return typeDefinition.Name;
                    }
                }
            }

            return _settings.EquipmentTypes.DefaultType;
        }

        /// <summary>
        /// Gets equipment code from equipment type name
        /// </summary>
        public string GetEquipmentCode(string equipmentType)
        {
            var typeDefinition = _settings.EquipmentTypes.Types
                .FirstOrDefault(t => t.Name.Equals(equipmentType, StringComparison.OrdinalIgnoreCase));

            return typeDefinition?.Code ?? equipmentType.Substring(0, Math.Min(2, equipmentType.Length)).ToUpper();
        }

        /// <summary>
        /// Resolves path template with environment variables
        /// </summary>
        public string ResolvePath(string pathTemplate)
        {
            if (string.IsNullOrEmpty(pathTemplate))
                return pathTemplate;

            var result = pathTemplate
                .Replace("{CommonApplicationData}", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
                .Replace("{LocalApplicationData}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Replace("{ApplicationData}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
                .Replace("{UserProfile}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                .Replace("{AppDataPath}", ResolvePath(_settings.Paths.AppDataPath));

            return result;
        }

        /// <summary>
        /// Gets the resolved logs path
        /// </summary>
        public string GetLogsPath() => ResolvePath(_settings.Paths.LogsPath);

        /// <summary>
        /// Gets the resolved drawings path
        /// </summary>
        public string GetDrawingsPath() => ResolvePath(_settings.Paths.DrawingsPath);

        /// <summary>
        /// Gets the resolved exports path
        /// </summary>
        public string GetExportsPath() => ResolvePath(_settings.Paths.ExportsPath);
    }
}
