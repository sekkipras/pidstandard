using ClosedXML.Excel;
using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Enums;

namespace PIDStandardization.Services
{
    /// <summary>
    /// Service for importing P&ID data from Excel format
    /// </summary>
    public class ExcelImportService
    {
        public class ImportResult
        {
            public int SuccessCount { get; set; }
            public int SkippedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> SkippedItems { get; set; } = new List<string>();
        }

        /// <summary>
        /// Import equipment from Excel file
        /// </summary>
        public ImportResult ImportEquipment(string filePath, Guid projectId, List<string> existingTags)
        {
            var result = new ImportResult();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet("Equipment");

                if (worksheet == null)
                {
                    result.Errors.Add("Worksheet 'Equipment' not found in Excel file");
                    return result;
                }

                // Start from row 4 (after headers at row 3)
                int row = 4;
                var equipmentList = new List<Equipment>();

                while (!worksheet.Cell(row, 1).IsEmpty())
                {
                    try
                    {
                        var tagNumber = worksheet.Cell(row, 1).GetString().Trim();

                        // Validate tag number
                        if (string.IsNullOrWhiteSpace(tagNumber))
                        {
                            result.Warnings.Add($"Row {row}: Tag number is empty, skipping");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        // Check for duplicates in existing database
                        if (existingTags.Contains(tagNumber))
                        {
                            result.SkippedItems.Add($"{tagNumber} (already exists)");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        // Check for duplicates in import file
                        if (equipmentList.Any(e => e.TagNumber == tagNumber))
                        {
                            result.Warnings.Add($"Row {row}: Duplicate tag '{tagNumber}' in import file, skipping");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        var equipment = new Equipment
                        {
                            EquipmentId = Guid.NewGuid(),
                            ProjectId = projectId,
                            TagNumber = tagNumber,
                            EquipmentType = worksheet.Cell(row, 2).GetString().Trim(),
                            Description = worksheet.Cell(row, 3).GetString().Trim(),
                            Service = worksheet.Cell(row, 4).GetString().Trim(),
                            Area = worksheet.Cell(row, 5).GetString().Trim(),
                            Manufacturer = worksheet.Cell(row, 7).GetString().Trim(),
                            Model = worksheet.Cell(row, 8).GetString().Trim(),
                            CreatedDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        // Parse status
                        var statusText = worksheet.Cell(row, 6).GetString().Trim();
                        if (Enum.TryParse<EquipmentStatus>(statusText, true, out var status))
                        {
                            equipment.Status = status;
                        }
                        else
                        {
                            equipment.Status = EquipmentStatus.Planned; // Default
                            if (!string.IsNullOrWhiteSpace(statusText))
                            {
                                result.Warnings.Add($"Row {row}: Invalid status '{statusText}', defaulting to Planned");
                            }
                        }

                        // Parse operating pressure
                        ParsePressure(worksheet.Cell(row, 9).GetString(),
                            out var opPressure, out var opPressureUnit);
                        equipment.OperatingPressure = opPressure;
                        equipment.OperatingPressureUnit = opPressureUnit;

                        // Parse operating temperature
                        ParseTemperature(worksheet.Cell(row, 10).GetString(),
                            out var opTemp, out var opTempUnit);
                        equipment.OperatingTemperature = opTemp;
                        equipment.OperatingTemperatureUnit = opTempUnit;

                        // Parse flow rate
                        ParseFlowRate(worksheet.Cell(row, 11).GetString(),
                            out var flowRate, out var flowRateUnit);
                        equipment.FlowRate = flowRate;
                        equipment.FlowRateUnit = flowRateUnit;

                        // Parse design pressure
                        ParsePressure(worksheet.Cell(row, 12).GetString(),
                            out var desPressure, out var desPressureUnit);
                        equipment.DesignPressure = desPressure;
                        equipment.DesignPressureUnit = desPressureUnit;

                        // Parse design temperature
                        ParseTemperature(worksheet.Cell(row, 13).GetString(),
                            out var desTemp, out var desTempUnit);
                        equipment.DesignTemperature = desTemp;
                        equipment.DesignTemperatureUnit = desTempUnit;

                        // Parse power/capacity
                        ParsePower(worksheet.Cell(row, 14).GetString(),
                            out var power, out var powerUnit);
                        equipment.PowerOrCapacity = power;
                        equipment.PowerOrCapacityUnit = powerUnit;

                        equipmentList.Add(equipment);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {row}: {ex.Message}");
                        result.ErrorCount++;
                    }

                    row++;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to open or read Excel file: {ex.Message}");
                result.ErrorCount++;
                return result;
            }
        }

        /// <summary>
        /// Import lines from Excel file
        /// </summary>
        public ImportResult ImportLines(string filePath, Guid projectId, List<string> existingLineNumbers, Dictionary<string, Guid> equipmentTagMap)
        {
            var result = new ImportResult();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet("Lines");

                if (worksheet == null)
                {
                    result.Errors.Add("Worksheet 'Lines' not found in Excel file");
                    return result;
                }

                // Start from row 4 (after headers at row 3)
                int row = 4;
                var linesList = new List<Line>();

                while (!worksheet.Cell(row, 1).IsEmpty())
                {
                    try
                    {
                        var lineNumber = worksheet.Cell(row, 1).GetString().Trim();

                        // Validate line number
                        if (string.IsNullOrWhiteSpace(lineNumber))
                        {
                            result.Warnings.Add($"Row {row}: Line number is empty, skipping");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        // Check for duplicates
                        if (existingLineNumbers.Contains(lineNumber))
                        {
                            result.SkippedItems.Add($"{lineNumber} (already exists)");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        if (linesList.Any(l => l.LineNumber == lineNumber))
                        {
                            result.Warnings.Add($"Row {row}: Duplicate line '{lineNumber}' in import file, skipping");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        var line = new Line
                        {
                            LineId = Guid.NewGuid(),
                            ProjectId = projectId,
                            LineNumber = lineNumber,
                            Service = worksheet.Cell(row, 2).GetString().Trim(),
                            FluidType = worksheet.Cell(row, 3).GetString().Trim(),
                            NominalSize = worksheet.Cell(row, 4).GetString().Trim(),
                            MaterialSpec = worksheet.Cell(row, 5).GetString().Trim(),
                            PipeSchedule = worksheet.Cell(row, 6).GetString().Trim(),
                            InsulationType = worksheet.Cell(row, 12).GetString().Trim()
                        };

                        // Parse design pressure
                        ParsePressure(worksheet.Cell(row, 7).GetString(),
                            out var desPressure, out var desPressureUnit);
                        line.DesignPressure = desPressure;
                        line.DesignPressureUnit = desPressureUnit;

                        // Parse design temperature
                        ParseTemperature(worksheet.Cell(row, 8).GetString(),
                            out var desTemp, out var desTempUnit);
                        line.DesignTemperature = desTemp;
                        line.DesignTemperatureUnit = desTempUnit;

                        // Parse from/to equipment
                        var fromEquipTag = worksheet.Cell(row, 9).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(fromEquipTag) && equipmentTagMap.ContainsKey(fromEquipTag))
                        {
                            line.FromEquipmentId = equipmentTagMap[fromEquipTag];
                        }
                        else if (!string.IsNullOrWhiteSpace(fromEquipTag))
                        {
                            result.Warnings.Add($"Row {row}: From equipment '{fromEquipTag}' not found in database");
                        }

                        var toEquipTag = worksheet.Cell(row, 10).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(toEquipTag) && equipmentTagMap.ContainsKey(toEquipTag))
                        {
                            line.ToEquipmentId = equipmentTagMap[toEquipTag];
                        }
                        else if (!string.IsNullOrWhiteSpace(toEquipTag))
                        {
                            result.Warnings.Add($"Row {row}: To equipment '{toEquipTag}' not found in database");
                        }

                        // Parse insulation required
                        var insulationReq = worksheet.Cell(row, 11).GetString().Trim();
                        line.InsulationRequired = insulationReq.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                                                  insulationReq.Equals("True", StringComparison.OrdinalIgnoreCase);

                        // Parse length
                        if (decimal.TryParse(worksheet.Cell(row, 13).GetString(), out var length))
                        {
                            line.Length = length;
                        }

                        linesList.Add(line);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {row}: {ex.Message}");
                        result.ErrorCount++;
                    }

                    row++;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to open or read Excel file: {ex.Message}");
                result.ErrorCount++;
                return result;
            }
        }

        /// <summary>
        /// Import instruments from Excel file
        /// </summary>
        public ImportResult ImportInstruments(string filePath, Guid projectId, List<string> existingInstrumentTags,
            Dictionary<string, Guid> equipmentTagMap, Dictionary<string, Guid> lineNumberMap)
        {
            var result = new ImportResult();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet("Instruments");

                if (worksheet == null)
                {
                    result.Errors.Add("Worksheet 'Instruments' not found in Excel file");
                    return result;
                }

                // Start from row 4 (after headers at row 3)
                int row = 4;
                var instrumentsList = new List<Instrument>();

                while (!worksheet.Cell(row, 1).IsEmpty())
                {
                    try
                    {
                        var tagNumber = worksheet.Cell(row, 1).GetString().Trim();

                        // Validate tag number
                        if (string.IsNullOrWhiteSpace(tagNumber))
                        {
                            result.Warnings.Add($"Row {row}: Tag number is empty, skipping");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        // Check for duplicates
                        if (existingInstrumentTags.Contains(tagNumber))
                        {
                            result.SkippedItems.Add($"{tagNumber} (already exists)");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        if (instrumentsList.Any(i => i.TagNumber == tagNumber))
                        {
                            result.Warnings.Add($"Row {row}: Duplicate tag '{tagNumber}' in import file, skipping");
                            result.SkippedCount++;
                            row++;
                            continue;
                        }

                        var instrument = new Instrument
                        {
                            InstrumentId = Guid.NewGuid(),
                            ProjectId = projectId,
                            TagNumber = tagNumber,
                            InstrumentType = worksheet.Cell(row, 2).GetString().Trim(),
                            MeasurementType = worksheet.Cell(row, 3).GetString().Trim(),
                            Units = worksheet.Cell(row, 6).GetString().Trim(),
                            Accuracy = worksheet.Cell(row, 7).GetString().Trim(),
                            ProcessConnection = worksheet.Cell(row, 8).GetString().Trim(),
                            OutputSignal = worksheet.Cell(row, 9).GetString().Trim(),
                            Location = worksheet.Cell(row, 12).GetString().Trim()
                        };

                        // Parse range min/max
                        if (decimal.TryParse(worksheet.Cell(row, 4).GetString(), out var rangeMin))
                        {
                            instrument.RangeMin = rangeMin;
                        }
                        if (decimal.TryParse(worksheet.Cell(row, 5).GetString(), out var rangeMax))
                        {
                            instrument.RangeMax = rangeMax;
                        }

                        // Parse equipment association
                        var equipTag = worksheet.Cell(row, 10).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(equipTag) && equipmentTagMap.ContainsKey(equipTag))
                        {
                            instrument.ParentEquipmentId = equipmentTagMap[equipTag];
                        }
                        else if (!string.IsNullOrWhiteSpace(equipTag))
                        {
                            result.Warnings.Add($"Row {row}: Equipment '{equipTag}' not found in database");
                        }

                        // Parse line association
                        var lineNum = worksheet.Cell(row, 11).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(lineNum) && lineNumberMap.ContainsKey(lineNum))
                        {
                            instrument.LineId = lineNumberMap[lineNum];
                        }
                        else if (!string.IsNullOrWhiteSpace(lineNum))
                        {
                            result.Warnings.Add($"Row {row}: Line '{lineNum}' not found in database");
                        }

                        instrumentsList.Add(instrument);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {row}: {ex.Message}");
                        result.ErrorCount++;
                    }

                    row++;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to open or read Excel file: {ex.Message}");
                result.ErrorCount++;
                return result;
            }
        }

        /// <summary>
        /// Generate import template Excel file
        /// </summary>
        public void GenerateEquipmentTemplate(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Equipment");

            // Title
            worksheet.Cell(1, 1).Value = "P&ID Equipment Import Template";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 16).Merge();

            // Instructions
            worksheet.Cell(2, 1).Value = "Fill in equipment data starting from row 4. Required fields: Tag Number, Equipment Type";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 16).Merge();

            // Column headers
            var headers = new[]
            {
                "Tag Number*", "Equipment Type*", "Description", "Service", "Area", "Status",
                "Manufacturer", "Model", "Operating Pressure", "Operating Temp", "Flow Rate",
                "Design Pressure", "Design Temp", "Power/Capacity", "Upstream Equipment", "Downstream Equipment"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Cell(3, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Example row
            worksheet.Cell(4, 1).Value = "P-101";
            worksheet.Cell(4, 2).Value = "Pump";
            worksheet.Cell(4, 3).Value = "Centrifugal pump";
            worksheet.Cell(4, 4).Value = "Cooling water";
            worksheet.Cell(4, 5).Value = "Area 1";
            worksheet.Cell(4, 6).Value = "Planned";
            worksheet.Cell(4, 7).Value = "ABC Corp";
            worksheet.Cell(4, 8).Value = "Model X";
            worksheet.Cell(4, 9).Value = "5.0 bar";
            worksheet.Cell(4, 10).Value = "25 °C";
            worksheet.Cell(4, 11).Value = "100 m3/h";
            worksheet.Cell(4, 12).Value = "10.0 bar";
            worksheet.Cell(4, 13).Value = "50 °C";
            worksheet.Cell(4, 14).Value = "15 kW";

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        public void GenerateLinesTemplate(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Lines");

            // Title
            worksheet.Cell(1, 1).Value = "P&ID Lines Import Template";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 13).Merge();

            // Instructions
            worksheet.Cell(2, 1).Value = "Fill in line data starting from row 4. Required fields: Line Number";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 13).Merge();

            // Column headers
            var headers = new[]
            {
                "Line Number*", "Service", "Fluid Type", "Nominal Size", "Material Spec",
                "Pipe Schedule", "Design Pressure", "Design Temp", "From Equipment",
                "To Equipment", "Insulation Required", "Insulation Type", "Length (m)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            // Example row
            worksheet.Cell(4, 1).Value = "L-100";
            worksheet.Cell(4, 2).Value = "Cooling water";
            worksheet.Cell(4, 3).Value = "Water";
            worksheet.Cell(4, 4).Value = "2\"";
            worksheet.Cell(4, 5).Value = "316SS";
            worksheet.Cell(4, 6).Value = "SCH 40";
            worksheet.Cell(4, 7).Value = "10.0 bar";
            worksheet.Cell(4, 8).Value = "50 °C";
            worksheet.Cell(4, 9).Value = "P-101";
            worksheet.Cell(4, 10).Value = "TK-100";
            worksheet.Cell(4, 11).Value = "Yes";
            worksheet.Cell(4, 12).Value = "Mineral wool";
            worksheet.Cell(4, 13).Value = "25.5";

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public void GenerateInstrumentsTemplate(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Instruments");

            // Title
            worksheet.Cell(1, 1).Value = "P&ID Instruments Import Template";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, 12).Merge();

            // Instructions
            worksheet.Cell(2, 1).Value = "Fill in instrument data starting from row 4. Required fields: Tag Number";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 12).Merge();

            // Column headers
            var headers = new[]
            {
                "Tag Number*", "Type", "Measurement Type", "Range Min", "Range Max",
                "Units", "Accuracy", "Process Connection", "Output Signal",
                "Equipment", "Line", "Location"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            // Example row
            worksheet.Cell(4, 1).Value = "PT-101";
            worksheet.Cell(4, 2).Value = "Pressure Transmitter";
            worksheet.Cell(4, 3).Value = "Pressure";
            worksheet.Cell(4, 4).Value = "0";
            worksheet.Cell(4, 5).Value = "10";
            worksheet.Cell(4, 6).Value = "bar";
            worksheet.Cell(4, 7).Value = "±0.5%";
            worksheet.Cell(4, 8).Value = "1/2\" NPT";
            worksheet.Cell(4, 9).Value = "4-20mA";
            worksheet.Cell(4, 10).Value = "P-101";
            worksheet.Cell(4, 11).Value = "L-100";
            worksheet.Cell(4, 12).Value = "Discharge";

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        // Helper methods for parsing values with units
        private void ParsePressure(string input, out decimal? value, out string unit)
        {
            ParseValueWithUnit(input, out value, out unit);
            if (string.IsNullOrWhiteSpace(unit) && value.HasValue)
                unit = "bar"; // Default unit
        }

        private void ParseTemperature(string input, out decimal? value, out string unit)
        {
            ParseValueWithUnit(input, out value, out unit);
            if (string.IsNullOrWhiteSpace(unit) && value.HasValue)
                unit = "°C"; // Default unit
        }

        private void ParseFlowRate(string input, out decimal? value, out string unit)
        {
            ParseValueWithUnit(input, out value, out unit);
            if (string.IsNullOrWhiteSpace(unit) && value.HasValue)
                unit = "m³/h"; // Default unit
        }

        private void ParsePower(string input, out decimal? value, out string unit)
        {
            ParseValueWithUnit(input, out value, out unit);
            if (string.IsNullOrWhiteSpace(unit) && value.HasValue)
                unit = "kW"; // Default unit
        }

        private void ParseValueWithUnit(string input, out decimal? value, out string unit)
        {
            value = null;
            unit = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return;

            // Split on space
            var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            // First part is the value
            if (decimal.TryParse(parts[0], out var parsedValue))
            {
                value = parsedValue;
            }

            // Second part (if exists) is the unit
            if (parts.Length > 1)
            {
                unit = string.Join(" ", parts.Skip(1));
            }
        }
    }
}
