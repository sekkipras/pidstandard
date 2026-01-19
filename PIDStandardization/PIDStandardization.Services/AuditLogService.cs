using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using System.Text.Json;

namespace PIDStandardization.Services
{
    /// <summary>
    /// Service for creating and managing audit log entries
    /// </summary>
    public class AuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Log an action performed on an entity
        /// </summary>
        public async Task LogActionAsync(
            string entityType,
            Guid entityId,
            string action,
            string performedBy,
            string changeDetails,
            object? oldValues = null,
            object? newValues = null,
            Guid? projectId = null,
            string? source = null)
        {
            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                PerformedBy = performedBy,
                Timestamp = DateTime.UtcNow,
                ChangeDetails = changeDetails,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                ProjectId = projectId,
                Source = source
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Log equipment creation
        /// </summary>
        public async Task LogEquipmentCreatedAsync(Equipment equipment, string performedBy, string? source = null)
        {
            await LogActionAsync(
                entityType: "Equipment",
                entityId: equipment.EquipmentId,
                action: "Created",
                performedBy: performedBy,
                changeDetails: $"Equipment '{equipment.TagNumber}' created",
                newValues: new
                {
                    equipment.TagNumber,
                    equipment.EquipmentType,
                    equipment.Description,
                    equipment.Status
                },
                projectId: equipment.ProjectId,
                source: source
            );
        }

        /// <summary>
        /// Log equipment update
        /// </summary>
        public async Task LogEquipmentUpdatedAsync(Equipment oldEquipment, Equipment newEquipment, string performedBy, string? source = null)
        {
            var changes = new List<string>();

            if (oldEquipment.TagNumber != newEquipment.TagNumber)
                changes.Add($"Tag: {oldEquipment.TagNumber} → {newEquipment.TagNumber}");
            if (oldEquipment.Description != newEquipment.Description)
                changes.Add($"Description changed");
            if (oldEquipment.Status != newEquipment.Status)
                changes.Add($"Status: {oldEquipment.Status} → {newEquipment.Status}");
            if (oldEquipment.Service != newEquipment.Service)
                changes.Add($"Service: {oldEquipment.Service} → {newEquipment.Service}");
            if (oldEquipment.Manufacturer != newEquipment.Manufacturer)
                changes.Add($"Manufacturer changed");
            if (oldEquipment.Model != newEquipment.Model)
                changes.Add($"Model changed");

            if (changes.Any())
            {
                await LogActionAsync(
                    entityType: "Equipment",
                    entityId: newEquipment.EquipmentId,
                    action: "Updated",
                    performedBy: performedBy,
                    changeDetails: string.Join(", ", changes),
                    oldValues: new
                    {
                        oldEquipment.TagNumber,
                        oldEquipment.Description,
                        oldEquipment.Status,
                        oldEquipment.Service,
                        oldEquipment.Manufacturer,
                        oldEquipment.Model
                    },
                    newValues: new
                    {
                        newEquipment.TagNumber,
                        newEquipment.Description,
                        newEquipment.Status,
                        newEquipment.Service,
                        newEquipment.Manufacturer,
                        newEquipment.Model
                    },
                    projectId: newEquipment.ProjectId,
                    source: source
                );
            }
        }

        /// <summary>
        /// Log equipment deletion
        /// </summary>
        public async Task LogEquipmentDeletedAsync(Equipment equipment, string performedBy, string? source = null)
        {
            await LogActionAsync(
                entityType: "Equipment",
                entityId: equipment.EquipmentId,
                action: "Deleted",
                performedBy: performedBy,
                changeDetails: $"Equipment '{equipment.TagNumber}' deleted",
                oldValues: new
                {
                    equipment.TagNumber,
                    equipment.EquipmentType,
                    equipment.Description
                },
                projectId: equipment.ProjectId,
                source: source
            );
        }

        /// <summary>
        /// Log batch tagging operation
        /// </summary>
        public async Task LogBatchTaggingAsync(int count, string performedBy, Guid projectId, string? source = null)
        {
            await LogActionAsync(
                entityType: "Equipment",
                entityId: Guid.Empty,
                action: "BatchTagged",
                performedBy: performedBy,
                changeDetails: $"Batch tagged {count} equipment items",
                projectId: projectId,
                source: source
            );
        }

        /// <summary>
        /// Log synchronization operation
        /// </summary>
        public async Task LogSynchronizationAsync(int addedToDB, int updatedInDB, int updatedInDrawing, string performedBy, Guid projectId, string? source = null)
        {
            var details = new List<string>();
            if (addedToDB > 0) details.Add($"{addedToDB} added to DB");
            if (updatedInDB > 0) details.Add($"{updatedInDB} updated in DB");
            if (updatedInDrawing > 0) details.Add($"{updatedInDrawing} updated in drawing");

            await LogActionAsync(
                entityType: "Equipment",
                entityId: Guid.Empty,
                action: "Synchronized",
                performedBy: performedBy,
                changeDetails: string.Join(", ", details),
                newValues: new { addedToDB, updatedInDB, updatedInDrawing },
                projectId: projectId,
                source: source
            );
        }

        /// <summary>
        /// Get audit logs for a specific entity
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId)
        {
            var logs = await _unitOfWork.AuditLogs.FindAsync(
                a => a.EntityType == entityType && a.EntityId == entityId
            );

            return logs.OrderByDescending(a => a.Timestamp);
        }

        /// <summary>
        /// Get recent audit logs for a project
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetProjectAuditLogsAsync(Guid projectId, int count = 100)
        {
            var logs = await _unitOfWork.AuditLogs.FindAsync(
                a => a.ProjectId == projectId
            );

            return logs.OrderByDescending(a => a.Timestamp).Take(count);
        }

        /// <summary>
        /// Get all audit logs within a date range
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate, Guid? projectId = null)
        {
            var logs = await _unitOfWork.AuditLogs.FindAsync(
                a => a.Timestamp >= startDate && a.Timestamp <= endDate &&
                     (projectId == null || a.ProjectId == projectId)
            );

            return logs.OrderByDescending(a => a.Timestamp);
        }
    }
}
