using ClosedXML.Excel;
using PIDStandardization.Core.Entities;

namespace PIDStandardization.Services
{
    /// <summary>
    /// Service for exporting P&ID data to Excel format
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// Export equipment list to Excel file
        /// </summary>
        public void ExportEquipment(IEnumerable<Equipment> equipment, string filePath, string projectName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Equipment");

            // Set up headers
            worksheet.Cell(1, 1).Value = $"P&ID Equipment List - {projectName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 16).Merge();

            // Column headers
            var headers = new[]
            {
                "Tag Number", "Equipment Type", "Description", "Service", "Area", "Status",
                "Manufacturer", "Model", "Operating Pressure", "Operating Temp", "Flow Rate",
                "Design Pressure", "Design Temp", "Power/Capacity", "Upstream Equipment", "Downstream Equipment"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data rows
            int row = 4;
            foreach (var eq in equipment)
            {
                worksheet.Cell(row, 1).Value = eq.TagNumber;
                worksheet.Cell(row, 2).Value = eq.EquipmentType;
                worksheet.Cell(row, 3).Value = eq.Description;
                worksheet.Cell(row, 4).Value = eq.Service;
                worksheet.Cell(row, 5).Value = eq.Area;
                worksheet.Cell(row, 6).Value = eq.Status.ToString();
                worksheet.Cell(row, 7).Value = eq.Manufacturer;
                worksheet.Cell(row, 8).Value = eq.Model;
                worksheet.Cell(row, 9).Value = eq.OperatingPressure.HasValue
                    ? $"{eq.OperatingPressure} {eq.OperatingPressureUnit}"
                    : "";
                worksheet.Cell(row, 10).Value = eq.OperatingTemperature.HasValue
                    ? $"{eq.OperatingTemperature} {eq.OperatingTemperatureUnit}"
                    : "";
                worksheet.Cell(row, 11).Value = eq.FlowRate.HasValue
                    ? $"{eq.FlowRate} {eq.FlowRateUnit}"
                    : "";
                worksheet.Cell(row, 12).Value = eq.DesignPressure.HasValue
                    ? $"{eq.DesignPressure} {eq.DesignPressureUnit}"
                    : "";
                worksheet.Cell(row, 13).Value = eq.DesignTemperature.HasValue
                    ? $"{eq.DesignTemperature} {eq.DesignTemperatureUnit}"
                    : "";
                worksheet.Cell(row, 14).Value = eq.PowerOrCapacity.HasValue
                    ? $"{eq.PowerOrCapacity} {eq.PowerOrCapacityUnit}"
                    : "";
                worksheet.Cell(row, 15).Value = eq.UpstreamEquipment?.TagNumber ?? "";
                worksheet.Cell(row, 16).Value = eq.DownstreamEquipment?.TagNumber ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save
            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Export lines list to Excel file
        /// </summary>
        public void ExportLines(IEnumerable<Line> lines, string filePath, string projectName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Lines");

            // Set up headers
            worksheet.Cell(1, 1).Value = $"P&ID Lines List - {projectName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 13).Merge();

            // Column headers
            var headers = new[]
            {
                "Line Number", "Service", "Fluid Type", "Nominal Size", "Material Spec",
                "Pipe Schedule", "Design Pressure", "Design Temp", "From Equipment",
                "To Equipment", "Insulation Required", "Insulation Type", "Length (m)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            // Data rows
            int row = 4;
            foreach (var line in lines)
            {
                worksheet.Cell(row, 1).Value = line.LineNumber;
                worksheet.Cell(row, 2).Value = line.Service;
                worksheet.Cell(row, 3).Value = line.FluidType;
                worksheet.Cell(row, 4).Value = line.NominalSize;
                worksheet.Cell(row, 5).Value = line.MaterialSpec;
                worksheet.Cell(row, 6).Value = line.PipeSchedule;
                worksheet.Cell(row, 7).Value = line.DesignPressure.HasValue
                    ? $"{line.DesignPressure} {line.DesignPressureUnit}"
                    : "";
                worksheet.Cell(row, 8).Value = line.DesignTemperature.HasValue
                    ? $"{line.DesignTemperature} {line.DesignTemperatureUnit}"
                    : "";
                worksheet.Cell(row, 9).Value = line.FromEquipment?.TagNumber ?? "";
                worksheet.Cell(row, 10).Value = line.ToEquipment?.TagNumber ?? "";
                worksheet.Cell(row, 11).Value = line.InsulationRequired ? "Yes" : "No";
                worksheet.Cell(row, 12).Value = line.InsulationType;
                worksheet.Cell(row, 13).Value = line.Length?.ToString() ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save
            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Export instruments list to Excel file
        /// </summary>
        public void ExportInstruments(IEnumerable<Instrument> instruments, string filePath, string projectName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Instruments");

            // Set up headers
            worksheet.Cell(1, 1).Value = $"P&ID Instruments List - {projectName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 12).Merge();

            // Column headers
            var headers = new[]
            {
                "Tag Number", "Type", "Measurement Type", "Range Min", "Range Max",
                "Units", "Accuracy", "Process Connection", "Output Signal",
                "Equipment", "Line", "Location"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            // Data rows
            int row = 4;
            foreach (var inst in instruments)
            {
                worksheet.Cell(row, 1).Value = inst.TagNumber;
                worksheet.Cell(row, 2).Value = inst.InstrumentType;
                worksheet.Cell(row, 3).Value = inst.MeasurementType;
                worksheet.Cell(row, 4).Value = inst.RangeMin?.ToString() ?? "";
                worksheet.Cell(row, 5).Value = inst.RangeMax?.ToString() ?? "";
                worksheet.Cell(row, 6).Value = inst.Units;
                worksheet.Cell(row, 7).Value = inst.Accuracy;
                worksheet.Cell(row, 8).Value = inst.ProcessConnection;
                worksheet.Cell(row, 9).Value = inst.OutputSignal;
                worksheet.Cell(row, 10).Value = inst.ParentEquipment?.TagNumber ?? "";
                worksheet.Cell(row, 11).Value = inst.Line?.LineNumber ?? "";
                worksheet.Cell(row, 12).Value = inst.Location;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save
            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Export drawings list to Excel file
        /// </summary>
        public void ExportDrawings(IEnumerable<Drawing> drawings, string filePath, string projectName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Drawings");

            // Set up headers
            worksheet.Cell(1, 1).Value = $"P&ID Drawings List - {projectName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 8).Merge();

            // Column headers
            var headers = new[]
            {
                "Drawing Number", "Title", "Revision", "Version", "File Name",
                "Import Date", "Imported By", "Equipment Count"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
            }

            // Data rows
            int row = 4;
            foreach (var drw in drawings)
            {
                worksheet.Cell(row, 1).Value = drw.DrawingNumber;
                worksheet.Cell(row, 2).Value = drw.DrawingTitle;
                worksheet.Cell(row, 3).Value = drw.Revision;
                worksheet.Cell(row, 4).Value = drw.VersionNumber.ToString();
                worksheet.Cell(row, 5).Value = drw.FileName;
                worksheet.Cell(row, 6).Value = drw.ImportDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 7).Value = drw.ImportedBy;
                worksheet.Cell(row, 8).Value = drw.Equipment?.Count.ToString() ?? "0";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Save
            workbook.SaveAs(filePath);
        }
    }
}
