using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PIDStandardization.Core.Configuration;

namespace PIDStandardization.AutoCAD.Models
{
    /// <summary>
    /// Represents equipment extracted from an AutoCAD drawing
    /// </summary>
    public class ExtractedEquipment
    {
        public ObjectId ObjectId { get; set; }
        public string BlockName { get; set; } = string.Empty;
        public Point3d Position { get; set; }
        public double Rotation { get; set; }
        public Scale3d ScaleFactors { get; set; }
        public string Layer { get; set; } = string.Empty;
        public bool IsTagged { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<string> ExtendedData { get; set; } = new List<string>();

        /// <summary>
        /// Gets the tag number from attributes or extended data
        /// </summary>
        public string? GetTagNumber()
        {
            // Get configured tag attribute names
            var tagAttributeNames = ConfigurationService.Instance.Settings.Tagging.TagAttributeNames;

            // Check attributes for configured tag attribute names
            foreach (var attrName in tagAttributeNames)
            {
                if (Attributes.ContainsKey(attrName.ToUpper()))
                    return Attributes[attrName.ToUpper()];
            }

            // Check extended data
            if (ExtendedData.Count > 1)
            {
                // Extended data format: ["TAGGED", "datetime", "tag_number"]
                return ExtendedData.Count >= 3 ? ExtendedData[2] : null;
            }

            return null;
        }

        /// <summary>
        /// Infers equipment type from block name using configuration
        /// </summary>
        public string GetEquipmentType()
        {
            return ConfigurationService.Instance.GetEquipmentType(BlockName);
        }
    }
}
