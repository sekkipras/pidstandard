using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Microsoft.Data.SqlClient;
using PIDStandardization.AutoCAD.Services;
using Serilog;
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
        public async void TagEquipment()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== P&ID Equipment Tagging ===");

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
                ed.WriteMessage($"\nSelected project: {selectedProject.ProjectName}");

                // Get all equipment for this project
                var allEquipment = await unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId && e.IsActive);
                ed.WriteMessage($"\nFound {allEquipment.Count()} existing equipment in database.");

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

                        // Generate suggested tag number
                        string suggestedTag = $"{blockRef.Name}-001";

                        // Count existing equipment with similar names to suggest next number
                        var similarEquipment = allEquipment.Where(e => e.TagNumber.StartsWith(blockRef.Name)).ToList();
                        if (similarEquipment.Any())
                        {
                            int maxNum = 0;
                            foreach (var eq in similarEquipment)
                            {
                                var parts = eq.TagNumber.Split('-');
                                if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int num))
                                {
                                    maxNum = Math.Max(maxNum, num);
                                }
                            }
                            suggestedTag = $"{blockRef.Name}-{(maxNum + 1):D3}";
                        }

                        // Show tag assignment dialog with project tagging mode
                        Forms.TagAssignmentForm tagForm = new Forms.TagAssignmentForm(
                            allEquipment,
                            blockRef.Name,
                            suggestedTag,
                            selectedProject.TaggingMode);

                        if (tagForm.ShowDialog() != System.Windows.Forms.DialogResult.OK || string.IsNullOrEmpty(tagForm.SelectedTagNumber))
                        {
                            ed.WriteMessage("\nTag assignment cancelled.");
                            tr.Commit();
                            return;
                        }

                        string assignedTag = tagForm.SelectedTagNumber;
                        ed.WriteMessage($"\nAssigned tag: {assignedTag}");

                        // Write tag to block attribute
                        BlockReference blockRefWrite = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as BlockReference;
                        if (blockRefWrite != null)
                        {
                            // Try to find and update TAG or TAGNUMBER attribute
                            bool attributeUpdated = false;
                            AttributeCollection attCol = blockRefWrite.AttributeCollection;

                            foreach (ObjectId attId in attCol)
                            {
                                AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                if (attRef != null && (attRef.Tag.ToUpper() == "TAG" || attRef.Tag.ToUpper() == "TAGNUMBER"))
                                {
                                    attRef.UpgradeOpen();
                                    attRef.TextString = assignedTag;
                                    attributeUpdated = true;
                                    ed.WriteMessage($"\nUpdated attribute '{attRef.Tag}' with tag number.");
                                    break;
                                }
                            }

                            if (!attributeUpdated)
                            {
                                ed.WriteMessage("\nWarning: Block has no TAG or TAGNUMBER attribute to update.");
                                ed.WriteMessage("\nTag will be saved to database but not visible in drawing.");
                            }

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

                            // Add extended data with tag information
                            ResultBuffer rb = new ResultBuffer(
                                new TypedValue((int)DxfCode.ExtendedDataRegAppName, "PIDSTD"),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "TAGGED"),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, assignedTag)
                            );

                            blockRefWrite.XData = rb;
                            ed.WriteMessage("\nBlock marked as tagged in extended data.");
                        }

                        // If not using existing equipment, create new equipment in database
                        if (!tagForm.UseExistingEquipment)
                        {
                            ed.WriteMessage("\nCreating new equipment in database...");

                            var equipment = new Core.Entities.Equipment
                            {
                                EquipmentId = Guid.NewGuid(),
                                ProjectId = selectedProject.ProjectId,
                                TagNumber = assignedTag,
                                EquipmentType = GetEquipmentTypeFromBlockName(blockRef.Name),
                                Description = $"Tagged from block {blockRef.Name}",
                                Area = blockRef.Layer,
                                SourceBlockName = blockRef.Name,
                                Status = Core.Enums.EquipmentStatus.Planned,
                                CreatedDate = DateTime.UtcNow,
                                IsActive = true
                            };

                            await unitOfWork.Equipment.AddAsync(equipment);
                            await unitOfWork.SaveChangesAsync();

                            ed.WriteMessage($"\nEquipment '{assignedTag}' created in database.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nUsing existing equipment '{assignedTag}' from database.");
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\nCommand completed successfully.");
            }
            catch (SqlException sqlEx)
            {
                Log.Error(sqlEx, "Database error in PIDTAG command");
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  DATABASE CONNECTION ERROR                            ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage("\n║  Cannot connect to database.                          ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Please check:                                        ║");
                ed.WriteMessage("\n║  • SQL Server is running                              ║");
                ed.WriteMessage("\n║  • Connection string in appsettings.json             ║");
                ed.WriteMessage("\n║  • Network connectivity                               ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
                ed.WriteMessage($"\nError Code: {sqlEx.Number}");
            }
            catch (System.Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in PIDTAG command", correlationId);
                ed.WriteMessage($"\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage($"\n║  ERROR                                                ║");
                ed.WriteMessage($"\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  {ex.Message,-52} ║");
                ed.WriteMessage($"\n║                                                       ║");
                ed.WriteMessage($"\n║  Error ID: {correlationId,-37} ║");
                ed.WriteMessage($"\n║  Check log file for details                           ║");
                ed.WriteMessage($"\n╚═══════════════════════════════════════════════════════╝");
            }
        }

        /// <summary>
        /// Command to batch tag multiple equipment blocks
        /// Usage: PIDBATCHTAG
        /// </summary>
        [CommandMethod("PIDBATCHTAG")]
        public async void BatchTagEquipment()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== P&ID Batch Equipment Tagging ===");

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
                ed.WriteMessage($"\nSelected project: {selectedProject.ProjectName}");

                // Get all equipment for this project
                var allEquipment = await unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId && e.IsActive);
                ed.WriteMessage($"\nFound {allEquipment.Count()} existing equipment in database.");

                // Prompt user to select multiple blocks
                ed.WriteMessage("\nSelect equipment blocks for batch tagging (press Enter when done)...");

                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\nSelect blocks: ";
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT")
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionResult psr = ed.GetSelection(pso, filter);

                if (psr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                SelectionSet selectionSet = psr.Value;
                int totalBlocks = selectionSet.Count;
                ed.WriteMessage($"\nSelected {totalBlocks} blocks for batch tagging.");

                // Get tag counters for auto-generation
                var tagCounters = new Dictionary<string, int>();
                foreach (var eq in allEquipment)
                {
                    var parts = eq.TagNumber.Split('-');
                    if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int num))
                    {
                        string prefix = string.Join("-", parts.Take(parts.Length - 1));
                        if (!tagCounters.ContainsKey(prefix) || tagCounters[prefix] < num)
                        {
                            tagCounters[prefix] = num;
                        }
                    }
                }

                // Process each selected block
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    // Register application name if not exists
                    RegAppTable rat = tr.GetObject(doc.Database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                    if (!rat.Has("PIDSTD"))
                    {
                        rat.UpgradeOpen();
                        RegAppTableRecord ratr = new RegAppTableRecord();
                        ratr.Name = "PIDSTD";
                        rat.Add(ratr);
                        tr.AddNewlyCreatedDBObject(ratr, true);
                    }

                    int taggedCount = 0;
                    int skippedCount = 0;
                    var newEquipmentList = new List<Core.Entities.Equipment>();

                    foreach (SelectedObject selObj in selectionSet)
                    {
                        if (selObj == null) continue;

                        BlockReference blockRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blockRef == null) continue;

                        // Check if block is already tagged (has PIDSTD XDATA)
                        bool alreadyTagged = false;
                        ResultBuffer existingXData = blockRef.GetXDataForApplication("PIDSTD");
                        if (existingXData != null)
                        {
                            TypedValue[] values = existingXData.AsArray();
                            foreach (var tv in values)
                            {
                                if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                                    tv.Value.ToString() == "TAGGED")
                                {
                                    alreadyTagged = true;
                                    break;
                                }
                            }
                            existingXData.Dispose();
                        }

                        if (alreadyTagged)
                        {
                            skippedCount++;
                            continue;
                        }

                        // Generate tag number
                        string blockName = blockRef.Name;
                        string tagPrefix = blockName;

                        // Initialize counter for this prefix if not exists
                        if (!tagCounters.ContainsKey(tagPrefix))
                        {
                            tagCounters[tagPrefix] = 0;
                        }

                        // Increment counter
                        tagCounters[tagPrefix]++;
                        string tagNumber = $"{tagPrefix}-{tagCounters[tagPrefix]:D3}";

                        // Upgrade block to write mode
                        blockRef.UpgradeOpen();

                        // Try to update TAG attribute
                        bool attributeUpdated = false;
                        AttributeCollection attCol = blockRef.AttributeCollection;

                        foreach (ObjectId attId in attCol)
                        {
                            AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                            if (attRef != null && (attRef.Tag.ToUpper() == "TAG" || attRef.Tag.ToUpper() == "TAGNUMBER"))
                            {
                                attRef.UpgradeOpen();
                                attRef.TextString = tagNumber;
                                attributeUpdated = true;
                                break;
                            }
                        }

                        // Add extended data with tag information
                        ResultBuffer rb = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, "PIDSTD"),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, "TAGGED"),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, tagNumber)
                        );

                        blockRef.XData = rb;

                        // Create new equipment in database
                        var equipment = new Core.Entities.Equipment
                        {
                            EquipmentId = Guid.NewGuid(),
                            ProjectId = selectedProject.ProjectId,
                            TagNumber = tagNumber,
                            EquipmentType = GetEquipmentTypeFromBlockName(blockRef.Name),
                            Description = $"Batch tagged from block {blockRef.Name}",
                            Area = blockRef.Layer,
                            SourceBlockName = blockRef.Name,
                            Status = Core.Enums.EquipmentStatus.Planned,
                            CreatedDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        newEquipmentList.Add(equipment);
                        taggedCount++;
                    }

                    tr.Commit();

                    // Save all new equipment to database
                    if (newEquipmentList.Any())
                    {
                        foreach (var equipment in newEquipmentList)
                        {
                            await unitOfWork.Equipment.AddAsync(equipment);
                        }
                        await unitOfWork.SaveChangesAsync();
                    }

                    ed.WriteMessage("\n\n╔═══════════════════════════════════════════╗");
                    ed.WriteMessage("\n║     Batch Tagging Summary                 ║");
                    ed.WriteMessage("\n╟───────────────────────────────────────────╢");
                    ed.WriteMessage($"\n║  Total Selected:       {totalBlocks,-20}║");
                    ed.WriteMessage($"\n║  Tagged Successfully:  {taggedCount,-20}║");
                    ed.WriteMessage($"\n║  Skipped (Already Tagged): {skippedCount,-16}║");
                    ed.WriteMessage("\n╚═══════════════════════════════════════════╝");
                    ed.WriteMessage($"\n\nAll {taggedCount} equipment items saved to database successfully!");
                }

                ed.WriteMessage("\nCommand completed successfully.");
            }
            catch (SqlException sqlEx)
            {
                Log.Error(sqlEx, "Database error in PIDBATCHTAG command");
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  DATABASE ERROR                                       ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage("\n║  Failed to save equipment to database.                ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Please check SQL Server connection and try again.    ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
                ed.WriteMessage($"\nDatabase Error Code: {sqlEx.Number}");
            }
            catch (System.Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in PIDBATCHTAG command", correlationId);
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  BATCH TAGGING ERROR                                  ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  Error ID: {correlationId,-37} ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Some equipment may have been tagged before error.    ║");
                ed.WriteMessage("\n║  Check log file for details.                          ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
            }
        }

        private string GetEquipmentTypeFromBlockName(string blockName)
        {
            string upperBlock = blockName.ToUpper();

            if (upperBlock.Contains("PUMP") || upperBlock.Contains("PMP") || upperBlock.StartsWith("P-"))
                return "Pump";
            if (upperBlock.Contains("VALVE") || upperBlock.Contains("VLV") || upperBlock.StartsWith("V-"))
                return "Valve";
            if (upperBlock.Contains("TANK") || upperBlock.StartsWith("TK") || upperBlock.StartsWith("T-"))
                return "Tank";
            if (upperBlock.Contains("VESSEL") || upperBlock.Contains("VSL") || upperBlock.StartsWith("VS"))
                return "Vessel";
            if (upperBlock.Contains("HX") || upperBlock.Contains("HEAT") || upperBlock.Contains("EXCHANGER"))
                return "Heat Exchanger";
            if (upperBlock.Contains("FILTER") || upperBlock.Contains("FLT") || upperBlock.StartsWith("F-"))
                return "Filter";
            if (upperBlock.Contains("COMPRESSOR") || upperBlock.Contains("COMP") || upperBlock.StartsWith("C-"))
                return "Compressor";
            if (upperBlock.Contains("SEPARATOR") || upperBlock.Contains("SEP") || upperBlock.StartsWith("S-"))
                return "Separator";

            return "Equipment";
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
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in PIDEXTRACT command", correlationId);
                ed.WriteMessage($"\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage($"\n║  EXTRACTION ERROR                                     ║");
                ed.WriteMessage($"\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  Failed to extract equipment from drawing.            ║");
                ed.WriteMessage($"\n║                                                       ║");
                ed.WriteMessage($"\n║  Error ID: {correlationId,-37} ║");
                ed.WriteMessage($"\n╚═══════════════════════════════════════════════════════╝");
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

                    // Get equipment type using pattern matching
                    string equipmentType = extracted.GetEquipmentType();

                    // Create new equipment
                    var equipment = new Core.Entities.Equipment
                    {
                        EquipmentId = Guid.NewGuid(),
                        ProjectId = selectedProject.ProjectId,
                        TagNumber = tagNumber,
                        EquipmentType = equipmentType,
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
            catch (SqlException sqlEx)
            {
                Log.Error(sqlEx, "Database error in PIDEXTRACTDB command");
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  DATABASE ERROR                                       ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage("\n║  Equipment extracted but failed to save to database.  ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Please check:                                        ║");
                ed.WriteMessage("\n║  • SQL Server is running                              ║");
                ed.WriteMessage("\n║  • Database exists and is accessible                  ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
            }
            catch (System.Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in PIDEXTRACTDB command", correlationId);
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  EXTRACTION TO DATABASE ERROR                         ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  Error ID: {correlationId,-37} ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Check log file for details.                          ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
            }
        }

        /// <summary>
        /// Command to visualize tag status in drawing
        /// Usage: PIDSTATUS
        /// </summary>
        [CommandMethod("PIDSTATUS")]
        public void ShowTagStatus()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== P&ID Tag Status Visualization ===");

                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    // Get RegAppTable to check if PIDSTD application exists
                    RegAppTable rat = tr.GetObject(doc.Database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                    bool hasPIDSTDApp = rat.Has("PIDSTD");

                    // Get block table
                    BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    int taggedCount = 0;
                    int untaggedCount = 0;
                    int totalBlocks = 0;

                    // Iterate through all block references in model space
                    foreach (ObjectId objId in modelSpace)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (ent is BlockReference blockRef)
                        {
                            // Skip layout blocks and viewports
                            BlockTableRecord btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btr.IsLayout || btr.IsAnonymous)
                                continue;

                            totalBlocks++;

                            // Check if block has PIDSTD extended data
                            bool isTagged = false;
                            if (hasPIDSTDApp)
                            {
                                ResultBuffer xdata = blockRef.GetXDataForApplication("PIDSTD");
                                if (xdata != null)
                                {
                                    TypedValue[] values = xdata.AsArray();
                                    // Check if XDATA contains "TAGGED" marker
                                    foreach (var tv in values)
                                    {
                                        if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                                            tv.Value.ToString() == "TAGGED")
                                        {
                                            isTagged = true;
                                            break;
                                        }
                                    }
                                    xdata.Dispose();
                                }
                            }

                            // Upgrade to write mode to change color
                            blockRef.UpgradeOpen();

                            if (isTagged)
                            {
                                // Set color to green (3)
                                blockRef.ColorIndex = 3;
                                taggedCount++;
                            }
                            else
                            {
                                // Set color to red (1)
                                blockRef.ColorIndex = 1;
                                untaggedCount++;
                            }
                        }
                    }

                    tr.Commit();

                    // Display statistics
                    ed.WriteMessage("\n\n╔═══════════════════════════════════════════════╗");
                    ed.WriteMessage("\n║        Tag Status Summary                     ║");
                    ed.WriteMessage("\n╟───────────────────────────────────────────────╢");
                    ed.WriteMessage($"\n║  Total Blocks:         {totalBlocks,-20}  ║");
                    ed.WriteMessage($"\n║  Tagged (Green):       {taggedCount,-20}  ║");
                    ed.WriteMessage($"\n║  Untagged (Red):       {untaggedCount,-20}  ║");

                    if (totalBlocks > 0)
                    {
                        double percentTagged = (double)taggedCount / totalBlocks * 100;
                        ed.WriteMessage($"\n║  Completion:           {percentTagged:F1}%-{new string(' ', 16)}║");
                    }

                    ed.WriteMessage("\n╟───────────────────────────────────────────────╢");
                    ed.WriteMessage("\n║  Legend:                                      ║");
                    ed.WriteMessage("\n║    Green blocks = Tagged in database          ║");
                    ed.WriteMessage("\n║    Red blocks   = Not yet tagged              ║");
                    ed.WriteMessage("\n╚═══════════════════════════════════════════════╝");

                    if (untaggedCount > 0)
                    {
                        ed.WriteMessage($"\n\nTip: Use PIDTAG command to tag individual blocks.");
                    }
                    else if (totalBlocks > 0)
                    {
                        ed.WriteMessage("\n\nAll blocks are tagged! Use PIDSYNC to verify database synchronization.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in PIDSTATUS command", correlationId);
                ed.WriteMessage($"\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage($"\n║  STATUS VISUALIZATION ERROR                           ║");
                ed.WriteMessage($"\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  Failed to display tag status.                        ║");
                ed.WriteMessage($"\n║                                                       ║");
                ed.WriteMessage($"\n║  Error ID: {correlationId,-37} ║");
                ed.WriteMessage($"\n╚═══════════════════════════════════════════════════════╝");
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
            ed.WriteMessage("\n║   Version 1.1                                             ║");
            ed.WriteMessage("\n╟───────────────────────────────────────────────────────────╢");
            ed.WriteMessage("\n║   Available Commands:                                     ║");
            ed.WriteMessage("\n║   PIDTAG       - Tag individual equipment block           ║");
            ed.WriteMessage("\n║   PIDBATCHTAG  - Tag multiple blocks at once              ║");
            ed.WriteMessage("\n║   PIDEXTRACT   - Extract all equipment from drawing       ║");
            ed.WriteMessage("\n║   PIDEXTRACTDB - Extract and save to database             ║");
            ed.WriteMessage("\n║   PIDSYNC      - Sync drawing with database               ║");
            ed.WriteMessage("\n║   PIDSTATUS    - Visualize tag status in drawing          ║");
            ed.WriteMessage("\n║   PIDINFO      - Show this information                    ║");
            ed.WriteMessage("\n╚═══════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Command to synchronize drawing with database (Bidirectional)
        /// Usage: PIDSYNC
        /// </summary>
        [CommandMethod("PIDSYNC")]
        public async void SyncWithDatabase()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== Bidirectional Synchronization ===");

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
                ed.WriteMessage($"\nSelected project: {selectedProject.ProjectName}");

                // Get all equipment for this project from database
                var databaseEquipment = (await unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId && e.IsActive)).ToList();
                ed.WriteMessage($"\nFound {databaseEquipment.Count} equipment in database.");

                // Extract equipment from current drawing
                var extractionService = new EquipmentExtractionService();
                var drawingEquipment = extractionService.ExtractEquipmentFromDrawing(doc.Database);
                ed.WriteMessage($"\nFound {drawingEquipment.Count} equipment in drawing.");

                // Build lookup dictionaries
                var drawingTagsDict = new Dictionary<string, Models.ExtractedEquipment>();
                foreach (var eq in drawingEquipment)
                {
                    string tag = eq.Attributes.ContainsKey("TAG") ? eq.Attributes["TAG"] :
                                eq.Attributes.ContainsKey("TAGNUMBER") ? eq.Attributes["TAGNUMBER"] :
                                eq.BlockName;

                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        drawingTagsDict[tag] = eq;
                    }
                }

                var databaseTagsDict = databaseEquipment.ToDictionary(e => e.TagNumber, e => e);

                // Analyze differences
                var newInDrawing = drawingTagsDict.Keys.Except(databaseTagsDict.Keys).ToList();
                var missingInDrawing = databaseTagsDict.Keys.Except(drawingTagsDict.Keys).ToList();
                var inBoth = drawingTagsDict.Keys.Intersect(databaseTagsDict.Keys).ToList();

                ed.WriteMessage("\n\n=== Synchronization Analysis ===");
                ed.WriteMessage($"\nNew in drawing (will add to database):  {newInDrawing.Count}");
                ed.WriteMessage($"\nMissing in drawing (in database only):  {missingInDrawing.Count}");
                ed.WriteMessage($"\nIn both (will check for updates):       {inBoth.Count}");

                if (newInDrawing.Count == 0 && missingInDrawing.Count == 0 && inBoth.Count == 0)
                {
                    ed.WriteMessage("\n\nNo equipment found. Nothing to synchronize.");
                    return;
                }

                // Show sync options
                ed.WriteMessage("\n\n=== Synchronization Options ===");
                ed.WriteMessage("\n1. Full Sync (Drawing → Database & Database → Drawing)");
                ed.WriteMessage("\n2. Drawing to Database only");
                ed.WriteMessage("\n3. Database to Drawing only");
                ed.WriteMessage("\n4. Cancel");

                PromptKeywordOptions pko = new PromptKeywordOptions("\nChoose sync direction");
                pko.Keywords.Add("Full");
                pko.Keywords.Add("ToDatabase");
                pko.Keywords.Add("ToDrawing");
                pko.Keywords.Add("Cancel");
                pko.Keywords.Default = "Full";

                PromptResult pr = ed.GetKeywords(pko);

                if (pr.Status != PromptStatus.OK || pr.StringResult == "Cancel")
                {
                    ed.WriteMessage("\nSync cancelled.");
                    return;
                }

                int addedToDB = 0;
                int updatedInDB = 0;
                int addedToDrawing = 0;
                int updatedInDrawing = 0;

                // Sync Drawing → Database
                if (pr.StringResult == "Full" || pr.StringResult == "ToDatabase")
                {
                    ed.WriteMessage("\n\n[Drawing → Database]");

                    // Add new equipment from drawing to database
                    foreach (var tag in newInDrawing)
                    {
                        var extracted = drawingTagsDict[tag];
                        var equipment = new Core.Entities.Equipment
                        {
                            EquipmentId = Guid.NewGuid(),
                            ProjectId = selectedProject.ProjectId,
                            TagNumber = tag,
                            EquipmentType = GetEquipmentTypeFromBlockName(extracted.BlockName),
                            Description = extracted.Attributes.ContainsKey("DESCRIPTION") ?
                                extracted.Attributes["DESCRIPTION"] :
                                $"Synced from drawing - Block: {extracted.BlockName}",
                            Area = extracted.Layer,
                            Service = extracted.Attributes.ContainsKey("SERVICE") ? extracted.Attributes["SERVICE"] : null,
                            Manufacturer = extracted.Attributes.ContainsKey("MANUFACTURER") ? extracted.Attributes["MANUFACTURER"] : null,
                            Model = extracted.Attributes.ContainsKey("MODEL") ? extracted.Attributes["MODEL"] : null,
                            SourceBlockName = extracted.BlockName,
                            Status = Core.Enums.EquipmentStatus.Planned,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        await unitOfWork.Equipment.AddAsync(equipment);
                        addedToDB++;
                    }

                    // Update existing equipment in database from drawing
                    foreach (var tag in inBoth)
                    {
                        var dbEquip = databaseTagsDict[tag];
                        var drawEquip = drawingTagsDict[tag];

                        // Update fields if they exist in drawing attributes
                        bool wasUpdated = false;

                        if (drawEquip.Attributes.ContainsKey("DESCRIPTION") &&
                            dbEquip.Description != drawEquip.Attributes["DESCRIPTION"])
                        {
                            dbEquip.Description = drawEquip.Attributes["DESCRIPTION"];
                            wasUpdated = true;
                        }

                        if (drawEquip.Attributes.ContainsKey("SERVICE") &&
                            dbEquip.Service != drawEquip.Attributes["SERVICE"])
                        {
                            dbEquip.Service = drawEquip.Attributes["SERVICE"];
                            wasUpdated = true;
                        }

                        if (drawEquip.Attributes.ContainsKey("MANUFACTURER") &&
                            dbEquip.Manufacturer != drawEquip.Attributes["MANUFACTURER"])
                        {
                            dbEquip.Manufacturer = drawEquip.Attributes["MANUFACTURER"];
                            wasUpdated = true;
                        }

                        if (drawEquip.Attributes.ContainsKey("MODEL") &&
                            dbEquip.Model != drawEquip.Attributes["MODEL"])
                        {
                            dbEquip.Model = drawEquip.Attributes["MODEL"];
                            wasUpdated = true;
                        }

                        if (dbEquip.Area != drawEquip.Layer)
                        {
                            dbEquip.Area = drawEquip.Layer;
                            wasUpdated = true;
                        }

                        if (wasUpdated)
                        {
                            dbEquip.ModifiedDate = DateTime.UtcNow;
                            updatedInDB++;
                        }
                    }

                    await unitOfWork.SaveChangesAsync();
                    ed.WriteMessage($"\n  Added {addedToDB} new equipment to database");
                    ed.WriteMessage($"\n  Updated {updatedInDB} existing equipment in database");
                }

                // Sync Database → Drawing
                if (pr.StringResult == "Full" || pr.StringResult == "ToDrawing")
                {
                    ed.WriteMessage("\n\n[Database → Drawing]");

                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        // Get RegAppTable
                        RegAppTable rat = tr.GetObject(doc.Database.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                        if (!rat.Has("PIDSTD"))
                        {
                            rat.UpgradeOpen();
                            RegAppTableRecord ratr = new RegAppTableRecord();
                            ratr.Name = "PIDSTD";
                            rat.Add(ratr);
                            tr.AddNewlyCreatedDBObject(ratr, true);
                        }

                        // Update existing blocks in drawing with database info
                        BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                        foreach (ObjectId objId in modelSpace)
                        {
                            Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                            if (ent is BlockReference blockRef)
                            {
                                BlockTableRecord btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                                if (btr.IsLayout || btr.IsAnonymous) continue;

                                // Check block tag
                                string blockTag = null;
                                AttributeCollection attCol = blockRef.AttributeCollection;
                                foreach (ObjectId attId in attCol)
                                {
                                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                    if (attRef != null && (attRef.Tag.ToUpper() == "TAG" || attRef.Tag.ToUpper() == "TAGNUMBER"))
                                    {
                                        blockTag = attRef.TextString;
                                        break;
                                    }
                                }

                                if (blockTag != null && databaseTagsDict.ContainsKey(blockTag))
                                {
                                    var dbEquip = databaseTagsDict[blockTag];
                                    bool wasUpdated = false;

                                    blockRef.UpgradeOpen();

                                    // Update attributes from database
                                    foreach (ObjectId attId in attCol)
                                    {
                                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                                        if (attRef == null) continue;

                                        string attTag = attRef.Tag.ToUpper();
                                        if (attTag == "DESCRIPTION" && !string.IsNullOrEmpty(dbEquip.Description))
                                        {
                                            attRef.TextString = dbEquip.Description;
                                            wasUpdated = true;
                                        }
                                        else if (attTag == "SERVICE" && !string.IsNullOrEmpty(dbEquip.Service))
                                        {
                                            attRef.TextString = dbEquip.Service;
                                            wasUpdated = true;
                                        }
                                        else if (attTag == "MANUFACTURER" && !string.IsNullOrEmpty(dbEquip.Manufacturer))
                                        {
                                            attRef.TextString = dbEquip.Manufacturer;
                                            wasUpdated = true;
                                        }
                                        else if (attTag == "MODEL" && !string.IsNullOrEmpty(dbEquip.Model))
                                        {
                                            attRef.TextString = dbEquip.Model;
                                            wasUpdated = true;
                                        }
                                    }

                                    // Update XDATA
                                    ResultBuffer rb = new ResultBuffer(
                                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, "PIDSTD"),
                                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, "TAGGED"),
                                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, blockTag),
                                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, $"DB_SYNC:{dbEquip.ModifiedDate:yyyyMMddHHmmss}")
                                    );
                                    blockRef.XData = rb;

                                    if (wasUpdated)
                                        updatedInDrawing++;
                                }
                            }
                        }

                        tr.Commit();
                    }

                    ed.WriteMessage($"\n  Updated {updatedInDrawing} blocks in drawing");
                }

                ed.WriteMessage("\n\n╔═══════════════════════════════════════════╗");
                ed.WriteMessage("\n║    Synchronization Complete               ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  To Database:   +{addedToDB,-4}  ~{updatedInDB,-4}         ║");
                ed.WriteMessage($"\n║  To Drawing:    +{addedToDrawing,-4}  ~{updatedInDrawing,-4}         ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════╝");
                ed.WriteMessage("\n  (+) Added    (~) Updated");
            }
            catch (SqlException sqlEx)
            {
                Log.Error(sqlEx, "Database error in PIDSYNC command");
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  SYNCHRONIZATION FAILED                               ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage("\n║  Database connection error during sync.               ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Changes may be partially applied.                    ║");
                ed.WriteMessage("\n║  Please check database connection and try again.      ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
                ed.WriteMessage($"\nDatabase Error Code: {sqlEx.Number}");
            }
            catch (System.Exception ex)
            {
                var correlationId = Guid.NewGuid();
                Log.Error(ex, "[{CorrelationId}] Error in PIDSYNC command", correlationId);
                ed.WriteMessage("\n╔═══════════════════════════════════════════════════════╗");
                ed.WriteMessage("\n║  SYNCHRONIZATION ERROR                                ║");
                ed.WriteMessage("\n╟───────────────────────────────────────────────────────╢");
                ed.WriteMessage($"\n║  Error ID: {correlationId,-37} ║");
                ed.WriteMessage("\n║                                                       ║");
                ed.WriteMessage("\n║  Sync may be incomplete. Check log for details.       ║");
                ed.WriteMessage("\n╚═══════════════════════════════════════════════════════╝");
            }
        }
    }
}
