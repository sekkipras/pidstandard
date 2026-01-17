namespace PIDStandardization.Data.Configuration
{
    /// <summary>
    /// Database configuration settings
    /// </summary>
    public class DatabaseConfiguration
    {
        public string ConnectionString { get; set; } = "Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;";
        public bool EnableSensitiveDataLogging { get; set; } = false;
        public int CommandTimeout { get; set; } = 30;
    }
}
