using Microsoft.Extensions.Configuration;

namespace PIDStandardization.Data.Configuration
{
    /// <summary>
    /// Database configuration settings
    /// </summary>
    public class DatabaseConfiguration
    {
        private static IConfigurationRoot? _configuration;
        private static readonly object _lock = new object();

        public string ConnectionString { get; set; }
        public bool EnableSensitiveDataLogging { get; set; }
        public int CommandTimeout { get; set; }

        /// <summary>
        /// Default constructor - loads settings from appsettings.json
        /// </summary>
        public DatabaseConfiguration()
        {
            LoadConfiguration();

            ConnectionString = _configuration?["DatabaseSettings:ConnectionString"]
                ?? "Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;";

            EnableSensitiveDataLogging = bool.TryParse(
                _configuration?["DatabaseSettings:EnableSensitiveDataLogging"],
                out bool enableLogging) ? enableLogging : false;

            CommandTimeout = int.TryParse(
                _configuration?["DatabaseSettings:CommandTimeout"],
                out int timeout) ? timeout : 30;
        }

        /// <summary>
        /// Constructor with explicit connection string (for testing/overrides)
        /// </summary>
        public DatabaseConfiguration(string connectionString)
        {
            ConnectionString = connectionString;
            EnableSensitiveDataLogging = false;
            CommandTimeout = 30;
        }

        private static void LoadConfiguration()
        {
            if (_configuration != null) return;

            lock (_lock)
            {
                if (_configuration != null) return;

                try
                {
                    // Try multiple paths to find appsettings.json
                    var basePath = AppDomain.CurrentDomain.BaseDirectory;
                    var configFilePath = Path.Combine(basePath, "appsettings.json");

                    // If not found in base directory, try parent directories
                    if (!File.Exists(configFilePath))
                    {
                        // Try AutoCAD plugin folder
                        var pluginPath = Path.Combine(basePath, "..", "appsettings.json");
                        if (File.Exists(pluginPath))
                        {
                            basePath = Path.GetDirectoryName(basePath) ?? basePath;
                        }
                    }

                    var builder = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                    _configuration = builder.Build();
                }
                catch
                {
                    // If configuration loading fails, _configuration remains null
                    // and default values will be used
                }
            }
        }

        /// <summary>
        /// Reload configuration from file (useful after config changes)
        /// </summary>
        public static void ReloadConfiguration()
        {
            lock (_lock)
            {
                _configuration = null;
            }
        }
    }
}
