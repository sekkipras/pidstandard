using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Serilog;
using System;
using System.IO;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(PIDStandardization.AutoCAD.PluginInitialization))]

namespace PIDStandardization.AutoCAD
{
    /// <summary>
    /// Plugin initialization class - loads when AutoCAD starts
    /// </summary>
    public class PluginInitialization : IExtensionApplication
    {
        /// <summary>
        /// Called when the plugin is loaded
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Configure Serilog for AutoCAD plugin
                ConfigureLogging();

                Log.Information("PID Standardization AutoCAD Plugin initializing");

                Document doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n╔═══════════════════════════════════════════════════════════╗");
                    doc.Editor.WriteMessage("\n║   P&ID Standardization Plugin Loaded                      ║");
                    doc.Editor.WriteMessage("\n║   Type PIDINFO for available commands                     ║");
                    doc.Editor.WriteMessage("\n╚═══════════════════════════════════════════════════════════╝\n");
                }

                Log.Information("PID Standardization AutoCAD Plugin loaded successfully");
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "PID Plugin initialization error");
                System.Diagnostics.Debug.WriteLine($"PID Plugin initialization error: {ex.Message}");
            }
        }

        private void ConfigureLogging()
        {
            // Create logs directory in ProgramData
            var logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PIDStandardization",
                "Logs");

            Directory.CreateDirectory(logsPath);

            // Configure Serilog for AutoCAD
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logsPath, "pidstandardization-autocad-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>
        /// Called when the plugin is unloaded
        /// </summary>
        public void Terminate()
        {
            Log.Information("PID Standardization AutoCAD Plugin unloading");

            // Dispose database service
            Services.DatabaseService.DisposeStatic();

            Log.CloseAndFlush();
        }
    }
}
