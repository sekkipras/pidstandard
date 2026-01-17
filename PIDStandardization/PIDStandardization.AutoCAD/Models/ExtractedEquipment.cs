using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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
            // First check attributes for common tag attribute names
            if (Attributes.ContainsKey("TAG"))
                return Attributes["TAG"];
            if (Attributes.ContainsKey("TAG_NUMBER"))
                return Attributes["TAG_NUMBER"];
            if (Attributes.ContainsKey("TAGNO"))
                return Attributes["TAGNO"];
            if (Attributes.ContainsKey("EQUIPMENT_TAG"))
                return Attributes["EQUIPMENT_TAG"];

            // Check extended data
            if (ExtendedData.Count > 1)
            {
                // Extended data format: ["TAGGED", "datetime", "tag_number"]
                return ExtendedData.Count >= 3 ? ExtendedData[2] : null;
            }

            return null;
        }

        /// <summary>
        /// Infers equipment type from block name
        /// </summary>
        public string GetEquipmentType()
        {
            string upperName = BlockName.ToUpper();

            if (upperName.Contains("PUMP") || upperName.Contains("PMP"))
                return "Pump";
            if (upperName.Contains("VALVE") || upperName.Contains("VLV"))
                return "Valve";
            if (upperName.Contains("TANK") || upperName.Contains("TK"))
                return "Tank";
            if (upperName.Contains("VESSEL") || upperName.Contains("VS"))
                return "Vessel";
            if (upperName.Contains("HX") || upperName.Contains("HEAT") || upperName.Contains("EXCHANGER"))
                return "Heat Exchanger";
            if (upperName.Contains("FILTER") || upperName.Contains("FLT"))
                return "Filter";
            if (upperName.Contains("COMP"))
                return "Compressor";
            if (upperName.Contains("SEP"))
                return "Separator";
            if (upperName.Contains("INST") || upperName.Contains("INSTRUMENT"))
                return "Instrument";

            return "Unknown";
        }
    }
}
