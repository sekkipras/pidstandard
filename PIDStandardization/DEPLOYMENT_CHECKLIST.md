# Production Deployment Checklist

## Overview

This checklist ensures a smooth deployment of the P&ID Standardization Application v1.0.4 to production environment.

---

## Pre-Deployment

### 1. Code Review ✓
- [x] All features implemented
- [x] Code committed to main branch
- [x] No pending changes
- [x] All commits have descriptive messages

**Latest Commits:**
- `b488128` - Quick reference card
- `6fade0e` - User guide and testing
- `ba9b786` - Implementation summary
- `495369d` - Hierarchical view
- `8dd4cf6` - Tag renumbering wizard
- `87e4957` - Audit trail
- `9d45dde` - Bidirectional sync

### 2. Documentation Review
- [ ] USER_GUIDE_AND_TESTING.md reviewed
- [ ] IMPLEMENTATION_SUMMARY.md reviewed
- [ ] MIGRATION_README.md reviewed
- [ ] QUICK_REFERENCE.md printed for users

### 3. Build Verification
```bash
# Build entire solution
cd PIDStandardization
dotnet build PIDStandardization.sln --configuration Release

# Verify output
ls -la PIDStandardization.UI/bin/Release/net8.0-windows/
ls -la PIDStandardization.AutoCAD/bin/Release/net8.0-windows/
```

**Checklist:**
- [ ] Solution builds without errors
- [ ] No warnings in Release build
- [ ] All dependencies resolved
- [ ] WPF executable created
- [ ] AutoCAD DLL created

---

## Database Deployment

### 1. Backup Current Database
```sql
-- Create backup before migration
BACKUP DATABASE [PIDStandardization]
TO DISK = 'C:\Backups\PIDStandardization_PreV104.bak'
WITH FORMAT, NAME = 'Pre-v1.0.4 Backup';
```

**Checklist:**
- [ ] Full database backup completed
- [ ] Backup verified and stored securely
- [ ] Backup location documented
- [ ] Rollback procedure tested

### 2. Apply Migration

**Option A: EF Core CLI (Recommended)**
```bash
cd PIDStandardization
dotnet ef database update --project PIDStandardization.Data --startup-project PIDStandardization.UI --configuration Release
```

**Option B: Manual SQL Script**
```sql
-- Run migration script from MIGRATION_README.md
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

**Verification:**
```sql
-- Verify table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'AuditLogs';

-- Verify indexes
SELECT * FROM sys.indexes
WHERE object_id = OBJECT_ID('AuditLogs');

-- Check permissions
SELECT HAS_PERMS_BY_NAME('AuditLogs', 'OBJECT', 'SELECT');
SELECT HAS_PERMS_BY_NAME('AuditLogs', 'OBJECT', 'INSERT');
```

**Checklist:**
- [ ] Migration executed successfully
- [ ] AuditLogs table created
- [ ] All 5 indexes created
- [ ] Foreign key constraint added
- [ ] Table permissions verified
- [ ] No errors in migration log

### 3. Update Connection String

**Production appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PROD_SERVER;Database=PIDStandardization;User Id=pid_app;Password=***;TrustServerCertificate=True;"
  }
}
```

**Location:**
- `PIDStandardization.Data/appsettings.json`
- `PIDStandardization.UI/appsettings.json`

**Checklist:**
- [ ] Production server name updated
- [ ] Production credentials configured
- [ ] Connection string tested
- [ ] Firewall rules configured
- [ ] SQL Server allows remote connections

---

## Application Deployment

### 1. WPF Application

**Deployment Steps:**
```bash
# Publish WPF app
dotnet publish PIDStandardization.UI/PIDStandardization.UI.csproj \
  --configuration Release \
  --output "./Publish/WPF" \
  --self-contained false

# Package for distribution
cd Publish/WPF
zip -r PIDStandardization_WPF_v1.0.4.zip *
```

**Installation Location:**
- Recommended: `C:\Program Files\PIDStandardization\WPF\`
- Alternate: Network share for multi-user access

**Checklist:**
- [ ] Application published
- [ ] Executable tested
- [ ] Dependencies included
- [ ] appsettings.json configured
- [ ] Desktop shortcut created
- [ ] Start menu entry added

### 2. AutoCAD Plugin

**Deployment Steps:**
```bash
# Copy AutoCAD DLL
cp PIDStandardization.AutoCAD/bin/Release/net8.0-windows/PIDStandardization.AutoCAD.dll \
   "C:/Program Files/Autodesk/AutoCAD 2026/Plug-ins/PIDStandardization/"

# Copy dependencies
cp PIDStandardization.AutoCAD/bin/Release/net8.0-windows/*.dll \
   "C:/Program Files/Autodesk/AutoCAD 2026/Plug-ins/PIDStandardization/"
```

**AutoCAD Load Configuration:**

Create `PIDStandardization.bundle` structure:
```
PIDStandardization.bundle/
├── Contents/
│   └── Windows/
│       └── PIDStandardization.AutoCAD.dll
└── PackageContents.xml
```

**PackageContents.xml:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage
  SchemaVersion="1.0"
  AutodeskProduct="AutoCAD"
  ProductType="Application"
  Name="PIDStandardization"
  Description="P&ID Equipment Standardization Plugin"
  AppVersion="1.0.4"
  Author="Your Company"
  HelpFile="./Help/PIDStandardization.chm"
  ProductCode="{YOUR-GUID-HERE}">

  <CompanyDetails
    Name="Your Company"
    Url="https://yourcompany.com"
    Email="support@yourcompany.com"/>

  <RuntimeRequirements
    OS="Win64"
    Platform="AutoCAD"
    SeriesMin="R24.0"/>

  <Components>
    <RuntimeRequirements
      OS="Win64"
      Platform="AutoCAD"
      SeriesMin="R24.0"
      SeriesMax="R24.2"/>
    <ComponentEntry
      AppName="PIDStandardization"
      ModuleName="./Contents/Windows/PIDStandardization.AutoCAD.dll"
      AppDescription="P&ID Standardization Commands"
      LoadOnCommandInvocation="False"
      LoadOnAutoCADStartup="True"/>
  </Components>
</ApplicationPackage>
```

**Installation Locations:**
- User: `%APPDATA%\Autodesk\ApplicationPlugins\`
- System: `C:\ProgramData\Autodesk\ApplicationPlugins\`

**Checklist:**
- [ ] DLL copied to plugins folder
- [ ] All dependencies included
- [ ] Bundle structure created
- [ ] PackageContents.xml configured
- [ ] Plugin loads on AutoCAD startup
- [ ] NETLOAD command tested
- [ ] All commands available (PIDINFO, PIDSYNC, etc.)

---

## User Training

### 1. Training Materials
- [ ] Quick Reference Card printed
- [ ] User Guide distributed
- [ ] Training session scheduled
- [ ] Demo project prepared
- [ ] Video tutorials recorded (optional)

### 2. Training Topics
- [ ] New PIDSYNC command and sync modes
- [ ] Audit Log usage and filtering
- [ ] Tag Renumbering Wizard workflow
- [ ] Hierarchical View navigation
- [ ] Integration with existing workflows

### 3. Hands-On Practice
- [ ] Create test project
- [ ] Practice sync operations
- [ ] Test tag renumbering
- [ ] Explore hierarchical view
- [ ] Review audit logs

---

## Post-Deployment Testing

### 1. Smoke Tests

**Test 1: WPF Application Launch**
```
1. Launch PIDStandardization.UI.exe
2. Verify project selection dialog appears
3. Select existing project
4. Verify main window opens
5. Check all tabs load
```
- [ ] Passed

**Test 2: Database Connection**
```
1. In WPF app, go to Equipment tab
2. Verify equipment loads
3. Add new equipment
4. Verify save successful
```
- [ ] Passed

**Test 3: AutoCAD Plugin Load**
```
1. Open AutoCAD
2. Check for plugin load message
3. Type: PIDINFO
4. Verify command executes
```
- [ ] Passed

**Test 4: PIDSYNC Command**
```
1. In AutoCAD: PIDSYNC
2. Select sync mode
3. Verify sync completes
4. Check statistics display
```
- [ ] Passed

**Test 5: Audit Log**
```
1. In WPF app, go to Audit Log tab
2. Select project
3. Verify logs display
4. Test filters
```
- [ ] Passed

**Test 6: Tag Renumbering**
```
1. Equipment → Tag Renumbering Wizard
2. Define pattern
3. Preview changes
4. Apply renumbering
5. Verify success
```
- [ ] Passed

**Test 7: Hierarchical View**
```
1. Equipment → Hierarchical View
2. Try all view modes
3. Select equipment
4. Verify details display
```
- [ ] Passed

### 2. Integration Tests
- [ ] Complete workflow: Extract → Tag → Sync → Renumber → Audit
- [ ] Multi-user concurrent access
- [ ] Large dataset performance (100+ equipment)

### 3. User Acceptance Testing (UAT)
- [ ] End users trained
- [ ] Test scenarios completed
- [ ] Feedback collected
- [ ] Issues documented
- [ ] Sign-off obtained

---

## Rollback Procedure

### If Issues Encountered:

**Step 1: Stop Applications**
- Close all WPF instances
- Close all AutoCAD sessions

**Step 2: Restore Database**
```sql
-- Restore from backup
USE master;
ALTER DATABASE [PIDStandardization] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE [PIDStandardization]
FROM DISK = 'C:\Backups\PIDStandardization_PreV104.bak'
WITH REPLACE;

ALTER DATABASE [PIDStandardization] SET MULTI_USER;
```

**Step 3: Revert Application**
- Restore previous WPF version
- Restore previous AutoCAD DLL
- Update connection strings if needed

**Step 4: Notify Users**
- Send rollback notification
- Schedule remediation plan
- Document issues for resolution

**Checklist:**
- [ ] Rollback procedure documented
- [ ] Rollback tested in staging
- [ ] Team trained on rollback
- [ ] Communication plan ready

---

## Go-Live Checklist

### Final Verification
- [ ] All pre-deployment tasks complete
- [ ] Database migration successful
- [ ] Applications deployed and tested
- [ ] Users trained
- [ ] Documentation distributed
- [ ] Support team briefed
- [ ] Monitoring configured
- [ ] Backup schedule verified

### Communication
- [ ] Users notified of new features
- [ ] Quick reference cards distributed
- [ ] Support channels announced
- [ ] Feedback mechanism established

### Monitoring
- [ ] Database performance baseline captured
- [ ] Application logs reviewed
- [ ] Error tracking enabled
- [ ] Usage metrics collection started

---

## Support Plan

### Week 1 Post-Deployment
- Daily check-ins with users
- Monitor audit log for errors
- Review database performance
- Collect feedback

### Week 2-4
- Weekly status meetings
- Address reported issues
- Fine-tune performance
- Update documentation as needed

### Ongoing
- Monthly review of audit logs
- Quarterly performance analysis
- Feature enhancement planning
- User satisfaction surveys

---

## Success Criteria

Deployment considered successful when:
- [ ] All features functional in production
- [ ] No critical bugs reported
- [ ] User acceptance achieved
- [ ] Performance meets benchmarks
- [ ] Audit trail capturing all changes
- [ ] No data integrity issues
- [ ] User training completed
- [ ] Documentation accurate and complete

---

## Sign-Off

### Technical Lead
- **Name:** ___________________________
- **Date:** ___________________________
- **Signature:** ___________________________

### Project Manager
- **Name:** ___________________________
- **Date:** ___________________________
- **Signature:** ___________________________

### End User Representative
- **Name:** ___________________________
- **Date:** ___________________________
- **Signature:** ___________________________

---

## Contact Information

**Support Team:**
- Email: support@yourcompany.com
- Phone: (xxx) xxx-xxxx
- Hours: Monday-Friday, 8 AM - 5 PM

**Emergency Contact:**
- Name: ___________________________
- Phone: ___________________________
- Email: ___________________________

---

**Deployment Version:** 1.0.4
**Deployment Date:** ___________________________
**Next Review Date:** ___________________________

---

## Notes

_Use this space to document deployment-specific notes, issues, or observations:_

---

**Document Version:** 1.0
**Last Updated:** 2026-01-18
**Prepared by:** Claude Sonnet 4.5
