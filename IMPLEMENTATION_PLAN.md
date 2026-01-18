# P&ID Standardization - Production Readiness Implementation Plan

> **Note**: This document describes the original implementation plan. Block learning features (Phase 5 and BlockMapping entity) have been removed from the current version and will be added in a future release after initial user testing.

## Overview
Transform the application from development/testing to production-ready for team deployment with:
1. Database enhancements for drawings ~~and block learning~~
2. Standalone application deployment
3. Welcome screen and user guidance
4. Drawing file management
5. ~~Block-to-equipment type learning~~ (Deferred to future release)
6. Excel import/export for valve/instrument lists

---

## Phase 1: Database Schema Enhancements

### 1.1 Enhance Drawing Entity
**Current State**: Drawing entity exists but missing fields
**Required Changes**:
```csharp
public class Drawing
{
    // Existing fields
    public Guid DrawingId { get; set; }
    public Guid ProjectId { get; set; }
    public string DrawingNumber { get; set; }
    public string? DrawingTitle { get; set; }
    public string? FilePath { get; set; }  // Path to original file
    public string? Revision { get; set; }
    public DateTime? RevisionDate { get; set; }
    public string? Status { get; set; }

    // NEW FIELDS TO ADD
    public string? FileName { get; set; }           // Original filename
    public string? StoredFilePath { get; set; }     // Path to copied file in app storage
    public DateTime ImportDate { get; set; }        // When it was imported
    public int VersionNumber { get; set; }          // Track versions (1, 2, 3...)
    public string? ImportedBy { get; set; }         // Who imported it
    public long? FileSizeBytes { get; set; }        // File size
    public string? FileHash { get; set; }           // MD5 hash to detect duplicates

    // Navigation
    public virtual Project? Project { get; set; }
    public virtual ICollection<Equipment> Equipment { get; set; }  // NEW: Equipment from this drawing
}
```

### 1.2 Create BlockMapping Entity (NEW)
**Purpose**: Learn block name → equipment type mappings
```csharp
public class BlockMapping
{
    public Guid BlockMappingId { get; set; }
    public string BlockName { get; set; }           // e.g., "PUMP-SYMBOL-01"
    public string EquipmentType { get; set; }       // e.g., "Pump"
    public int UsageCount { get; set; }             // How many times used
    public DateTime FirstUsedDate { get; set; }
    public DateTime LastUsedDate { get; set; }
    public double ConfidenceScore { get; set; }     // 0.0 to 1.0
    public string? CreatedBy { get; set; }
    public bool IsUserConfirmed { get; set; }       // True if user manually confirmed
}
```

### 1.3 Update Equipment Entity
**Add field**:
```csharp
public Guid? DrawingId { get; set; }                // NEW: Link to source drawing
public string? SourceBlockName { get; set; }        // NEW: Original AutoCAD block name
public virtual Drawing? SourceDrawing { get; set; } // NEW: Navigation property
```

### 1.4 Migration Strategy
- Create new migration: `Add_DrawingEnhancements_And_BlockMappings`
- Steps:
  1. Add new columns to Drawings table
  2. Create BlockMappings table
  3. Add DrawingId and SourceBlockName to Equipment table
  4. Add foreign key constraint: Equipment.DrawingId → Drawings.DrawingId

---

## Phase 2: Application Settings & Configuration

### 2.1 Application Settings File
**Location**: `%AppData%\PIDStandardization\appsettings.json`
**Structure**:
```json
{
  "Drawing Storage": {
    "BasePath": "C:\\ProgramData\\PIDStandardization\\Drawings",
    "MaxFileSizeMB": 50,
    "AllowedExtensions": [".dwg", ".dxf"]
  },
  "BlockLearning": {
    "Enabled": true,
    "AutoSuggestThreshold": 0.7,
    "LocalMappingsPath": "%AppData%\\PIDStandardization\\block_mappings.json"
  },
  "UserInterface": {
    "ShowWelcomeOnStartup": true,
    "Theme": "Light",
    "LastSelectedProjectId": null
  },
  "Database": {
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=PIDStandardization;..."
  }
}
```

### 2.2 Local Block Mappings File
**Location**: `%AppData%\PIDStandardization\block_mappings.json`
**Structure**:
```json
{
  "mappings": [
    {
      "blockName": "PUMP-CENTRIFUGAL",
      "equipmentType": "Pump",
      "usageCount": 15,
      "lastUsed": "2025-01-17T10:30:00Z",
      "confidence": 0.95
    }
  ],
  "lastSyncToDatabase": "2025-01-17T09:00:00Z"
}
```

---

## Phase 3: Welcome Screen & Help System

### 3.1 Welcome Dialog (WPF)
**File**: `PIDStandardization.UI/Views/WelcomeDialog.xaml`
**Features**:
- Application logo/title
- Quick Start Guide (5 steps):
  1. Create a new project
  2. Install AutoCAD plugin
  3. Extract equipment from P&ID
  4. Review and validate tags
  5. Export to Excel
- Checkbox: "Don't show this again"
- Buttons: "Get Started", "View Documentation"

### 3.2 Help Menu Integration
Add to MainWindow menu:
```xml
<MenuItem Header="_Help">
    <MenuItem Header="_Welcome Screen" Click="ShowWelcome_Click"/>
    <MenuItem Header="_Quick Start Guide" Click="ShowQuickStart_Click"/>
    <MenuItem Header="_Documentation" Click="ShowDocumentation_Click"/>
    <MenuItem Header="_About" Click="ShowAbout_Click"/>
</MenuItem>
```

### 3.3 Documentation
Create PDF: `User_Guide.pdf` with:
- Installation instructions
- AutoCAD plugin setup
- Creating projects
- Importing drawings
- Tag extraction workflow
- Excel import/export
- Troubleshooting

---

## Phase 4: Drawing Management System

### 4.1 Drawing Import Feature
**UI Location**: Drawings tab in MainWindow
**Workflow**:
1. User clicks "Import Drawing"
2. File picker opens (.dwg, .dxf files)
3. System:
   - Calculates file hash (MD5)
   - Checks if already imported (duplicate detection)
   - If new version: increment VersionNumber
   - Copies file to `{AppData}\PIDStandardization\Drawings\{ProjectName}\{DrawingNumber}_v{Version}.dwg`
   - Creates Drawing record in database
4. Display success message with version info

### 4.2 Drawing List View
**UI Components**:
- DataGrid showing:
  - Drawing Number
  - Title
  - Revision
  - Version
  - Import Date
  - Equipment Count (from this drawing)
  - File Size
- Buttons:
  - Import Drawing
  - View in AutoCAD (if installed)
  - Export Equipment List
  - Delete Drawing

### 4.3 Drawing-Equipment Linking
**When extracting from AutoCAD** (PIDEXTRACTDB command):
1. Prompt user: "Select drawing this extraction is from"
2. Show list of drawings for project
3. Link all extracted equipment to selected DrawingId
4. Store original block name in SourceBlockName field

---

## Phase 5: Block Learning System

### 5.1 Learning Workflow (AutoCAD Plugin)
**Modified PIDEXTRACTDB**:
```
For each extracted block:
  1. Check local block_mappings.json
  2. If mapping exists with confidence > 0.7:
     - Auto-fill EquipmentType
     - Show "(suggested)" indicator
  3. Else:
     - Show "Unknown" type
  4. User can:
     - Accept suggestion
     - Change to different type
  5. On save:
     - Update/create mapping in local JSON
     - Increment usage count
     - Update confidence score
```

### 5.2 Confidence Score Calculation
```
confidence = min(1.0, usageCount / 10) * userConfirmedMultiplier
where userConfirmedMultiplier = 1.0 if confirmed, 0.8 if auto
```

### 5.3 Sync to Database (Future Enhancement)
- Manual "Sync Mappings" button in UI
- Uploads local JSON mappings to BlockMappings table
- Merges with existing mappings (keep highest confidence)

---

## Phase 6: Excel Import/Export

### 6.1 Excel Library
**NuGet Package**: EPPlus or ClosedXML
**Reason**: Both support .xlsx, easy to use, well-documented

### 6.2 Import Valve/Instrument Lists
**UI**: Equipment tab → "Import from Excel" button
**Workflow**:
1. User selects Excel file
2. System reads first row as headers
3. Maps columns:
   - Tag Number (required)
   - Equipment Type / Valve Type / Instrument Type
   - Size
   - Pressure Rating
   - Material
   - Service
   - Location / Area
4. Preview import (grid with 10 rows)
5. User confirms → bulk insert to Equipment table

**Template Files** (provide to users):
- `Valve_List_Template.xlsx`
- `Instrument_List_Template.xlsx`

### 6.3 Export to Excel
**UI**: Equipment tab → "Export to Excel" button
**Features**:
- Filter by equipment type (checkboxes)
- Generate workbook with sheets:
  - Sheet 1: "All Equipment"
  - Sheet 2: "Pumps" (if selected)
  - Sheet 3: "Valves" (if selected)
  - Sheet 4: "Instruments" (if selected)
  - Sheet 5: "Summary" (counts by type, status)
- Columns:
  - Tag Number
  - Equipment Type
  - Description
  - Service
  - Area
  - Size (for valves)
  - Pressure Rating (for valves)
  - Material (for valves)
  - Range (for instruments)
  - Accuracy (for instruments)
  - Status
  - Source Drawing
  - Created Date

---

## Phase 7: Application Deployment

### 7.1 Publish Configuration
**Project**: PIDStandardization.UI
**Settings**:
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <PublishTrimmed>false</PublishTrimmed>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

**Command**:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 7.2 Deployment Package Contents
```
PIDStandardization_Installer/
├── PIDStandardization.UI.exe          (main application)
├── AutoCAD_Plugin/
│   └── PIDStandardization.AutoCAD.dll
│   └── Installation_Instructions.pdf
├── Templates/
│   ├── Valve_List_Template.xlsx
│   └── Instrument_List_Template.xlsx
├── Documentation/
│   └── User_Guide.pdf
└── README.txt                          (quick start)
```

### 7.3 Installation Instructions
**README.txt**:
```
P&ID Standardization Application v1.0
====================================

Installation:
1. Run PIDStandardization.UI.exe
2. Application will create database on first run
3. Follow Welcome Screen instructions

AutoCAD Plugin Installation:
1. Open AutoCAD 2026
2. Type NETLOAD
3. Browse to: AutoCAD_Plugin\PIDStandardization.AutoCAD.dll
4. Click Load
5. Type PIDINFO to verify installation

Support: contact@company.com
```

---

## Implementation Order

### Priority 1 (This Session):
1. ✅ Database migrations (Phase 1)
2. ✅ Welcome screen (Phase 3.1)
3. ✅ Drawing import basic functionality (Phase 4.1)

### Priority 2 (Next Session):
4. Block learning in AutoCAD (Phase 5.1)
5. Excel export (Phase 6.3)
6. Drawing list view (Phase 4.2)

### Priority 3 (Polish):
7. Excel import (Phase 6.2)
8. Help documentation (Phase 3.3)
9. Deployment package (Phase 7)

---

## Technical Decisions

### Why Local JSON for Block Mappings Initially?
- **Pro**: Fast, no network dependency, works offline
- **Pro**: Easy to implement, no schema changes needed yet
- **Pro**: Can sync to database later when tested
- **Con**: Not shared across team initially
- **Decision**: Start local, add database sync in future

### Why EPPlus/ClosedXML for Excel?
- **EPPlus**: More features, better for complex exports
- **ClosedXML**: Simpler API, better for basic read/write
- **Decision**: Use ClosedXML for easier learning curve

### Drawing File Storage Location?
- Option 1: `%AppData%` - Per user, backed up with roaming profiles
- Option 2: `%ProgramData%` - Shared across users, requires admin
- Option 3: Network share - Centralized but depends on network
- **Decision**: `%ProgramData%\PIDStandardization\Drawings\` for team sharing

### Self-Contained vs Framework-Dependent?
- **Self-Contained**: Larger (200MB+), but works on any PC
- **Framework-Dependent**: Smaller (5MB), requires .NET 8 installed
- **Decision**: Self-contained for easier deployment to team

---

## Database Migration SQL Preview

```sql
-- Add columns to Drawings table
ALTER TABLE Drawings ADD
    FileName NVARCHAR(255) NULL,
    StoredFilePath NVARCHAR(500) NULL,
    ImportDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    VersionNumber INT NOT NULL DEFAULT 1,
    ImportedBy NVARCHAR(100) NULL,
    FileSizeBytes BIGINT NULL,
    FileHash NVARCHAR(32) NULL;

-- Create BlockMappings table
CREATE TABLE BlockMappings (
    BlockMappingId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BlockName NVARCHAR(100) NOT NULL,
    EquipmentType NVARCHAR(50) NOT NULL,
    UsageCount INT NOT NULL DEFAULT 1,
    FirstUsedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastUsedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ConfidenceScore FLOAT NOT NULL DEFAULT 0.5,
    CreatedBy NVARCHAR(100) NULL,
    IsUserConfirmed BIT NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IX_BlockMappings_BlockName
    ON BlockMappings(BlockName);

-- Add columns to Equipment table
ALTER TABLE Equipment ADD
    DrawingId UNIQUEIDENTIFIER NULL,
    SourceBlockName NVARCHAR(100) NULL;

ALTER TABLE Equipment ADD CONSTRAINT FK_Equipment_Drawing
    FOREIGN KEY (DrawingId) REFERENCES Drawings(DrawingId)
    ON DELETE SET NULL;
```

---

## Success Criteria

### Phase 1:
- ✅ Database migration runs without errors
- ✅ New tables/columns visible in SQL Server Management Studio
- ✅ Existing data preserved

### Phase 2:
- ✅ Application creates settings file on first run
- ✅ Settings persist between sessions

### Phase 3:
- ✅ Welcome dialog shows on first launch
- ✅ User can disable welcome screen
- ✅ Help menu accessible

### Phase 4:
- ✅ User can import .dwg file
- ✅ File copied to storage location
- ✅ Drawing record created in database
- ✅ Version tracking works for duplicate imports

### Phase 5:
- ✅ Block mappings JSON created
- ✅ AutoCAD suggests equipment type based on block name
- ✅ User can correct suggestions
- ✅ Confidence scores increase with usage

### Phase 6:
- ✅ User can export equipment to Excel
- ✅ Separate sheets for different equipment types
- ✅ File opens correctly in Excel

### Phase 7:
- ✅ Single .exe file runs on clean Windows machine
- ✅ AutoCAD plugin loads successfully
- ✅ Documentation is clear and helpful

---

## End of Plan
