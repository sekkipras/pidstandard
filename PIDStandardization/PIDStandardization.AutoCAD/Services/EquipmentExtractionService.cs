using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PIDStandardization.AutoCAD.Models;

namespace PIDStandardization.AutoCAD.Services
{
    /// <summary>
    /// Service for extracting equipment information from AutoCAD drawings
    /// </summary>
    public class EquipmentExtractionService
    {
        /// <summary>
        /// Equipment block prefixes that identify P&ID equipment
        /// </summary>
        private readonly string[] _equipmentPrefixes = new[]
        {
            "PUMP", "PMP", "P-",
            "VALVE", "VLV", "V-",
            "TANK", "TK", "T-",
            "VESSEL", "VS", "VSL",
            "HX", "HEAT", "EXCHANGER",
            "FILTER", "FLT", "F-",
            "COMPRESSOR", "COMP", "C-",
            "SEPARATOR", "SEP", "S-",
            "INSTRUMENT", "INST", "I-"
        };

        /// <summary>
        /// Extracts all equipment blocks from the drawing
        /// </summary>
        public List<ExtractedEquipment> ExtractEquipmentFromDrawing(Database db)
        {
            var equipmentList = new List<ExtractedEquipment>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in modelSpace)
                {
                    if (objId.ObjectClass.DxfName == "INSERT")
                    {
                        BlockReference blockRef = tr.GetObject(objId, OpenMode.ForRead) as BlockReference;

                        if (blockRef != null && IsEquipmentBlock(blockRef.Name))
                        {
                            var equipment = new ExtractedEquipment
                            {
                                ObjectId = objId,
                                BlockName = blockRef.Name,
                                Position = blockRef.Position,
                                Rotation = blockRef.Rotation,
                                ScaleFactors = blockRef.ScaleFactors,
                                Layer = blockRef.Layer
                            };

                            // Extract attributes if present
                            if (blockRef.AttributeCollection.Count > 0)
                            {
                                foreach (ObjectId attId in blockRef.AttributeCollection)
                                {
                                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                    if (attRef != null)
                                    {
                                        equipment.Attributes[attRef.Tag] = attRef.TextString;
                                    }
                                }
                            }

                            // Check for extended data (tags assigned by PIDTAG command)
                            ResultBuffer xdata = blockRef.GetXDataForApplication("PIDSTD");
                            if (xdata != null)
                            {
                                equipment.IsTagged = true;
                                foreach (TypedValue tv in xdata)
                                {
                                    if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                                    {
                                        equipment.ExtendedData.Add(tv.Value?.ToString() ?? "");
                                    }
                                }
                            }

                            equipmentList.Add(equipment);
                        }
                    }
                }

                tr.Commit();
            }

            return equipmentList;
        }

        /// <summary>
        /// Determines if a block name represents equipment
        /// </summary>
        private bool IsEquipmentBlock(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return false;

            string upperName = blockName.ToUpper();

            // Check against known prefixes
            foreach (var prefix in _equipmentPrefixes)
            {
                if (upperName.Contains(prefix))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts attributes from a specific block reference
        /// </summary>
        public Dictionary<string, string> ExtractBlockAttributes(ObjectId blockRefId, Database db)
        {
            var attributes = new Dictionary<string, string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tr.GetObject(blockRefId, OpenMode.ForRead) as BlockReference;

                if (blockRef != null && blockRef.AttributeCollection.Count > 0)
                {
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (attRef != null)
                        {
                            attributes[attRef.Tag] = attRef.TextString;
                        }
                    }
                }

                tr.Commit();
            }

            return attributes;
        }
    }
}
