using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
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
                Document doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n╔═══════════════════════════════════════════════════════════╗");
                    doc.Editor.WriteMessage("\n║   P&ID Standardization Plugin Loaded                      ║");
                    doc.Editor.WriteMessage("\n║   Type PIDINFO for available commands                     ║");
                    doc.Editor.WriteMessage("\n╚═══════════════════════════════════════════════════════════╝\n");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PID Plugin initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the plugin is unloaded
        /// </summary>
        public void Terminate()
        {
            // Cleanup code here if needed
        }
    }
}
