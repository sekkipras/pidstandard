using System.Text.Json;

namespace PIDStandardization.AutoCAD.Services
{
    /// <summary>
    /// Service for learning and suggesting equipment types based on AutoCAD block names
    /// </summary>
    public class BlockLearningService
    {
        private readonly string _mappingsFilePath;
        private Dictionary<string, BlockMappingInfo> _blockMappings;

        public BlockLearningService()
        {
            // Store mappings in AppData\PIDStandardization
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "PIDStandardization");
            Directory.CreateDirectory(appFolder);

            _mappingsFilePath = Path.Combine(appFolder, "block_mappings.json");
            _blockMappings = new Dictionary<string, BlockMappingInfo>();

            LoadMappings();
        }

        /// <summary>
        /// Get suggested equipment type for a block name
        /// </summary>
        public BlockSuggestion GetSuggestion(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return new BlockSuggestion { BlockName = blockName, Confidence = 0.0 };

            // Normalize block name (uppercase, trim)
            blockName = blockName.ToUpperInvariant().Trim();

            if (_blockMappings.TryGetValue(blockName, out var mapping))
            {
                return new BlockSuggestion
                {
                    BlockName = blockName,
                    SuggestedEquipmentType = mapping.EquipmentType,
                    Confidence = mapping.ConfidenceScore,
                    UsageCount = mapping.UsageCount
                };
            }

            // No mapping found
            return new BlockSuggestion
            {
                BlockName = blockName,
                Confidence = 0.0
            };
        }

        /// <summary>
        /// Learn or update a mapping between block name and equipment type
        /// </summary>
        public void LearnMapping(string blockName, string equipmentType, bool userConfirmed = false)
        {
            if (string.IsNullOrWhiteSpace(blockName) || string.IsNullOrWhiteSpace(equipmentType))
                return;

            // Normalize inputs
            blockName = blockName.ToUpperInvariant().Trim();
            equipmentType = equipmentType.Trim();

            if (_blockMappings.TryGetValue(blockName, out var existing))
            {
                // Update existing mapping
                existing.UsageCount++;
                existing.LastUsedDate = DateTime.UtcNow;

                if (userConfirmed)
                {
                    existing.IsUserConfirmed = true;
                }

                // Update confidence score
                existing.ConfidenceScore = CalculateConfidence(existing.UsageCount, existing.IsUserConfirmed);
            }
            else
            {
                // Create new mapping
                _blockMappings[blockName] = new BlockMappingInfo
                {
                    BlockName = blockName,
                    EquipmentType = equipmentType,
                    UsageCount = 1,
                    FirstUsedDate = DateTime.UtcNow,
                    LastUsedDate = DateTime.UtcNow,
                    IsUserConfirmed = userConfirmed,
                    ConfidenceScore = CalculateConfidence(1, userConfirmed)
                };
            }

            SaveMappings();
        }

        /// <summary>
        /// Calculate confidence score based on usage count and user confirmation
        /// </summary>
        private double CalculateConfidence(int usageCount, bool isUserConfirmed)
        {
            // Base confidence from usage: max out at 10 usages
            double baseConfidence = Math.Min(1.0, usageCount / 10.0);

            // Multiplier for user confirmation
            double multiplier = isUserConfirmed ? 1.0 : 0.8;

            return baseConfidence * multiplier;
        }

        /// <summary>
        /// Load mappings from JSON file
        /// </summary>
        private void LoadMappings()
        {
            try
            {
                if (File.Exists(_mappingsFilePath))
                {
                    var json = File.ReadAllText(_mappingsFilePath);
                    var mappings = JsonSerializer.Deserialize<List<BlockMappingInfo>>(json);

                    if (mappings != null)
                    {
                        _blockMappings = mappings.ToDictionary(m => m.BlockName, m => m);
                    }
                }
            }
            catch (Exception ex)
            {
                // If loading fails, start with empty mappings
                System.Diagnostics.Debug.WriteLine($"Error loading block mappings: {ex.Message}");
                _blockMappings = new Dictionary<string, BlockMappingInfo>();
            }
        }

        /// <summary>
        /// Save mappings to JSON file
        /// </summary>
        private void SaveMappings()
        {
            try
            {
                var mappingsList = _blockMappings.Values.OrderByDescending(m => m.ConfidenceScore).ToList();
                var json = JsonSerializer.Serialize(mappingsList, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_mappingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving block mappings: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all mappings sorted by confidence
        /// </summary>
        public List<BlockMappingInfo> GetAllMappings()
        {
            return _blockMappings.Values.OrderByDescending(m => m.ConfidenceScore).ToList();
        }
    }

    /// <summary>
    /// Information about a block-to-equipment-type mapping
    /// </summary>
    public class BlockMappingInfo
    {
        public string BlockName { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public DateTime FirstUsedDate { get; set; }
        public DateTime LastUsedDate { get; set; }
        public bool IsUserConfirmed { get; set; }
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Suggestion result for a block name
    /// </summary>
    public class BlockSuggestion
    {
        public string BlockName { get; set; } = string.Empty;
        public string? SuggestedEquipmentType { get; set; }
        public double Confidence { get; set; }
        public int UsageCount { get; set; }
    }
}
