# Database Migration Instructions

## Audit Trail Migration

The audit trail feature has been implemented with database schema changes. To apply these changes to your database, follow these steps:

### Option 1: Using EF Core CLI (Recommended for Development)

From a Windows machine with .NET SDK installed:

```bash
cd PIDStandardization
dotnet ef database update --project PIDStandardization.Data --startup-project PIDStandardization.UI
```

### Option 2: SQL Script Execution

If you prefer to run the migration via SQL scripts or cannot use the .NET CLI, use the migration file:
- Location: `PIDStandardization.Data/Migrations/20260118180000_AddAuditLog.cs`
- This migration creates the `AuditLogs` table with the following structure:
  - AuditLogId (uniqueidentifier, PK)
  - EntityType (nvarchar(50)) - e.g., Equipment, Line, Instrument
  - EntityId (uniqueidentifier) - ID of the entity being tracked
  - Action (nvarchar(50)) - e.g., Created, Updated, Deleted
  - PerformedBy (nvarchar(100)) - username
  - Timestamp (datetime2)
  - ChangeDetails (nvarchar(2000)) - description of changes
  - OldValues (nvarchar(max)) - JSON snapshot of old state
  - NewValues (nvarchar(max)) - JSON snapshot of new state
  - ProjectId (uniqueidentifier, nullable, FK to Projects)
  - Source (nvarchar(100), nullable) - IP/machine name

### Option 3: Manual SQL

Alternatively, execute this SQL directly against your database:

```sql
CREATE TABLE [dbo].[AuditLogs] (
    [AuditLogId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [EntityType] NVARCHAR(50) NOT NULL,
    [EntityId] UNIQUEIDENTIFIER NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [PerformedBy] NVARCHAR(100) NOT NULL,
    [Timestamp] DATETIME2 NOT NULL,
    [ChangeDetails] NVARCHAR(2000) NOT NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [ProjectId] UNIQUEIDENTIFIER NULL,
    [Source] NVARCHAR(100) NULL,
    CONSTRAINT [FK_AuditLogs_Projects_ProjectId] FOREIGN KEY ([ProjectId])
        REFERENCES [Projects] ([ProjectId]) ON DELETE SET NULL
);

CREATE INDEX [IX_AuditLogs_EntityId] ON [AuditLogs] ([EntityId]);
CREATE INDEX [IX_AuditLogs_EntityType] ON [AuditLogs] ([EntityType]);
CREATE INDEX [IX_AuditLogs_ProjectId] ON [AuditLogs] ([ProjectId]);
CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
CREATE INDEX [IX_AuditLogs_EntityType_EntityId_Timestamp]
    ON [AuditLogs] ([EntityType], [EntityId], [Timestamp]);
```

## Features Added

### Database Changes
- New `AuditLogs` table for tracking all changes
- Indexes for efficient querying by entity, timestamp, and project

### Backend Services
- `AuditLogService` - Service layer for logging and querying audit entries
- Integration with `IUnitOfWork` pattern
- Methods for logging:
  - Equipment creation/update/deletion
  - Batch tagging operations
  - Synchronization events
  - Generic entity tracking

### UI Components
- New "Audit Log" tab in main application window
- Filtering by:
  - Entity type (Equipment, Line, Instrument, Drawing, Project)
  - Action type (Created, Updated, Deleted, Batch Tagged, Synchronized)
  - Time range (24 Hours, 7 Days, 30 Days, All Time)
- Real-time audit log viewer with sortable columns

### Integration Points
- AutoCAD commands can now log operations via `DatabaseService.GetAuditLogService()`
- WPF dialogs have access to audit logging through dependency injection
- Automatic tracking of who made changes and when

## Usage Examples

### In AutoCAD Commands:
```csharp
var auditService = Services.DatabaseService.GetAuditLogService();
await auditService.LogEquipmentCreatedAsync(
    equipment,
    Environment.UserName,
    Environment.MachineName
);
```

### In WPF Application:
```csharp
var auditService = serviceProvider.GetRequiredService<AuditLogService>();
await auditService.LogEquipmentUpdatedAsync(
    oldEquipment,
    newEquipment,
    Environment.UserName,
    null
);
```

## Next Steps

After running the migration:
1. Verify the `AuditLogs` table exists in your database
2. Test the audit log viewer in the WPF application
3. Confirm audit entries are being created for CRUD operations
