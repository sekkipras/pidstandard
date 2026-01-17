using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PIDStandardization.AutoCAD.Services;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PIDStandardization.AutoCAD.Commands
{
    /// <summary>
    /// AutoCAD commands for P&ID Standardization
    /// </summary>
    public class PIDCommands
    {
        /// <summary>
        /// Command to tag equipment in the drawing
        /// Usage: PIDTAG
        /// </summary>
        [CommandMethod("PIDTAG")]
        public void TagEquipment()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== P&ID Equipment Tagging ===");
                ed.WriteMessage("\nSelect equipment block to tag...");

                // Prompt user to select a block
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect equipment block: ");
                peo.SetRejectMessage("\nOnly blocks allowed.");
                peo.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    BlockReference blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;

                    if (blockRef != null)
                    {
                        ed.WriteMessage($"\nSelected block: {blockRef.Name}");
                        ed.WriteMessage($"\nPosition: X={blockRef.Position.X:F2}, Y={blockRef.Position.Y:F2}");

                        // TODO: Open dialog to assign tag number from database
                        ed.WriteMessage("\n[Feature under development: Tag assignment dialog will open here]");

                        // For now, just add extended data to mark this as tagged
                        BlockReference blockRefWrite = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as BlockReference;
                        if (blockRefWrite != null)
                        {
                            // Add application name to RegAppTable if not exists
                            RegAppTable rat = tr.GetObject(doc.Database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                            if (!rat.Has("PIDSTD"))
                            {
                                rat.UpgradeOpen();
                                RegAppTableRecord ratr = new RegAppTableRecord();
                                ratr.Name = "PIDSTD";
                                rat.Add(ratr);
                                tr.AddNewlyCreatedDBObject(ratr, true);
                            }

                            // Add extended data
                            ResultBuffer rb = new ResultBuffer(
                                new TypedValue((int)DxfCode.ExtendedDataRegAppName, "PIDSTD"),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "TAGGED"),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                            );

                            blockRefWrite.XData = rb;
                            ed.WriteMessage("\nBlock marked as tagged.");
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\nCommand completed successfully.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to extract all equipment from the drawing
        /// Usage: PIDEXTRACT
        /// </summary>
        [CommandMethod("PIDEXTRACT")]
        public void ExtractEquipment()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== P&ID Equipment Extraction ===");

                var extractionService = new EquipmentExtractionService();
                var equipmentList = extractionService.ExtractEquipmentFromDrawing(doc.Database);

                ed.WriteMessage($"\n\nFound {equipmentList.Count} equipment items:");

                foreach (var eq in equipmentList)
                {
                    ed.WriteMessage($"\n  Block: {eq.BlockName} at ({eq.Position.X:F2}, {eq.Position.Y:F2})");
                }

                ed.WriteMessage("\n\nCommand completed.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to extract and save equipment to database
        /// Usage: PIDEXTRACTDB
        /// </summary>
        [CommandMethod("PIDEXTRACTDB")]
        public async void ExtractAndSaveEquipment()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== P&ID Equipment Extraction to Database ===");

                // Get projects from database
                var unitOfWork = Services.DatabaseService.GetUnitOfWork();
                var projects = await unitOfWork.Projects.GetAllAsync();

                if (!projects.Any())
                {
                    ed.WriteMessage("\nNo projects found in database. Please create a project first in the WPF application.");
                    return;
                }

                // Show project selection dialog
                Forms.ProjectSelectionForm projectForm = new Forms.ProjectSelectionForm(projects);
                if (projectForm.ShowDialog() != System.Windows.Forms.DialogResult.OK || projectForm.SelectedProject == null)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                var selectedProject = projectForm.SelectedProject;
                ed.WriteMessage($"\nSelected project: {selectedProject.ProjectName} (Tagging Mode: {selectedProject.TaggingMode})");

                // Get drawings for the selected project
                var drawings = await unitOfWork.Drawings.FindAsync(d => d.ProjectId == selectedProject.ProjectId);
                Core.Entities.Drawing selectedDrawing = null;

                if (drawings.Any())
                {
                    // Show drawing selection dialog
                    Forms.DrawingSelectionForm drawingForm = new Forms.DrawingSelectionForm(drawings);
                    if (drawingForm.ShowDialog() == System.Windows.Forms.DialogResult.OK && drawingForm.SelectedDrawing != null)
                    {
                        selectedDrawing = drawingForm.SelectedDrawing;
                        ed.WriteMessage($"\nSelected drawing: {selectedDrawing.DrawingNumber}");
                    }
                    else
                    {
                        ed.WriteMessage("\nNo drawing selected. Equipment will be saved without drawing reference.");
                    }
                }
                else
                {
                    ed.WriteMessage("\nNo drawings found for this project. Equipment will be saved without drawing reference.");
                }

                // Extract equipment from drawing
                var extractionService = new EquipmentExtractionService();
                var equipmentList = extractionService.ExtractEquipmentFromDrawing(doc.Database);

                ed.WriteMessage($"\nFound {equipmentList.Count} equipment items");
                ed.WriteMessage("\nSaving to database...");

                int savedCount = 0;
                int skippedCount = 0;
                var tagCounters = new Dictionary<string, int>();

                foreach (var extracted in equipmentList)
                {
                    // Generate unique tag number
                    // First check if block has a TAG attribute
                    string tagNumber = extracted.Attributes.ContainsKey("TAG") || extracted.Attributes.ContainsKey("TAGNUMBER")
                        ? (extracted.Attributes.ContainsKey("TAG") ? extracted.Attributes["TAG"] : extracted.Attributes["TAGNUMBER"])
                        : null;

                    // If no TAG attribute, generate from block name with counter
                    if (string.IsNullOrWhiteSpace(tagNumber))
                    {
                        string baseTag = extracted.BlockName;
                        if (!tagCounters.ContainsKey(baseTag))
                        {
                            tagCounters[baseTag] = 1;
                        }
                        tagNumber = $"{baseTag}-{tagCounters[baseTag]:D3}";
                        tagCounters[baseTag]++;
                    }

                    // Check if equipment already exists
                    var existingEquipment = await unitOfWork.Equipment.FindAsync(
                        e => e.ProjectId == selectedProject.ProjectId && e.TagNumber == tagNumber);

                    if (existingEquipment.Any())
                    {
                        skippedCount++;
                        continue;
                    }

                    // Create new equipment
                    var equipment = new Core.Entities.Equipment
                    {
                        EquipmentId = Guid.NewGuid(),
                        ProjectId = selectedProject.ProjectId,
                        TagNumber = tagNumber,
                        EquipmentType = extracted.GetEquipmentType(),
                        Description = $"Extracted from drawing at ({extracted.Position.X:F2}, {extracted.Position.Y:F2})",
                        Area = extracted.Layer,
                        Status = Core.Enums.EquipmentStatus.Planned,
                        DrawingId = selectedDrawing?.DrawingId,
                        SourceBlockName = extracted.BlockName,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    };

                    await unitOfWork.Equipment.AddAsync(equipment);
                    savedCount++;
                }

                await unitOfWork.SaveChangesAsync();

                ed.WriteMessage($"\n\n✓ Extraction complete!");
                ed.WriteMessage($"\n  Saved: {savedCount} equipment items");
                ed.WriteMessage($"\n  Skipped (already exists): {skippedCount} items");
                ed.WriteMessage("\n\nYou can now view and manage these equipment in the WPF application.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
                ed.WriteMessage($"\nStack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Command to show P&ID Standardization info
        /// Usage: PIDINFO
        /// </summary>
        [CommandMethod("PIDINFO")]
        public void ShowInfo()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            ed.WriteMessage("\n╔═══════════════════════════════════════════════════════════╗");
            ed.WriteMessage("\n║   P&ID Standardization Application for AutoCAD            ║");
            ed.WriteMessage("\n║   Version 1.0                                             ║");
            ed.WriteMessage("\n╟───────────────────────────────────────────────────────────╢");
            ed.WriteMessage("\n║   Available Commands:                                     ║");
            ed.WriteMessage("\n║   PIDTAG       - Tag equipment block                      ║");
            ed.WriteMessage("\n║   PIDEXTRACT   - Extract all equipment from drawing       ║");
            ed.WriteMessage("\n║   PIDEXTRACTDB - Extract and save to database             ║");
            ed.WriteMessage("\n║   PIDSYNC      - Sync drawing with database               ║");
            ed.WriteMessage("\n║   PIDINFO      - Show this information                    ║");
            ed.WriteMessage("\n╚═══════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Command to synchronize drawing with database
        /// Usage: PIDSYNC
        /// </summary>
        [CommandMethod("PIDSYNC")]
        public void SyncWithDatabase()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            ed.WriteMessage("\n=== Synchronize Drawing with Database ===");
            ed.WriteMessage("\n[Feature under development]");
            ed.WriteMessage("\nThis will sync tagged equipment with the central database.");
        }
    }
}
