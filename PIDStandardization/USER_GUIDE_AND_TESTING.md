# User Guide and Testing Checklist

## What is PID Standardization?

**PID Standardization** is an integrated software solution designed to streamline the management and standardization of Process & Instrumentation Diagrams (P&IDs) in engineering projects. The system bridges the gap between AutoCAD drawings and structured database management, enabling engineers to extract equipment data from P&ID drawings, standardize tag numbering, synchronize changes bidirectionally between drawings and database, and maintain comprehensive audit trails of all modifications.

The solution consists of two main components:
1. **WPF Desktop Application** - A Windows application for managing projects, equipment, lines, instruments, and drawings with features like hierarchical visualization, tag renumbering wizards, audit logging, and Excel import/export capabilities.
2. **AutoCAD Plugin** - A .NET plugin that integrates directly into AutoCAD 2026, providing commands to extract equipment from drawings, assign tags, and synchronize data bidirectionally with the database.

Together, these components enable engineering teams to maintain consistency across P&ID documentation, enforce standardized tagging conventions, track all changes for compliance and audit purposes, and collaborate efficiently across multiple drawings and team members.

---

## Quick Start Guide

This guide will walk you through testing each new feature implemented in the latest release. Follow the checklist to ensure everything works correctly in your environment.

---

## Pre-Testing Setup

**IMPORTANT:** This setup is required **ONLY** in the following scenarios:
- **First-time installation** on a new computer
- **After pulling updates** from GitHub that include database schema changes (migrations)
- **When the database needs to be recreated** or reset

Once completed, you do NOT need to repeat this setup every time you use the application. Simply launch the WPF application or load the AutoCAD plugin as normal.

### 1. Database Migration ✓

Before testing any features, you must apply the database migration:

**Option A: Using EF Core CLI (Recommended)**
```bash
cd PIDStandardization
dotnet ef database update --project PIDStandardization.Data --startup-project PIDStandardization.UI
```

**Option B: Manual SQL Execution**
See [MIGRATION_README.md](MIGRATION_README.md) for SQL scripts.

**Verification:**
- [ ] Migration completed without errors
- [ ] `AuditLogs` table exists in database
- [ ] All indexes created successfully

### 2. Build and Deploy

```bash
# Build all projects
dotnet build PIDStandardization.sln

# Verify AutoCAD plugin DLL
# Check: PIDStandardization.AutoCAD/bin/Debug/net8.0-windows/PIDStandardization.AutoCAD.dll
```

**Verification:**
- [ ] Solution builds without errors
- [ ] No warnings about missing references
- [ ] AutoCAD plugin DLL generated
- [ ] WPF application executable created

---

## Daily Usage (After Setup Complete)

Once the pre-testing setup is complete, you can use the application normally without repeating those steps:

### Starting the WPF Application
1. Navigate to: `PIDStandardization.UI/bin/Debug/net8.0-windows/`
2. Run: `PIDStandardization.UI.exe`
3. The application connects to the database automatically
4. Select your project and start working

### Loading the AutoCAD Plugin
1. Open AutoCAD 2026
2. Type command: `NETLOAD`
3. Browse to: `PIDStandardization.AutoCAD/bin/Debug/net8.0-windows/PIDStandardization.AutoCAD.dll`
4. Click "Load"
5. Plugin commands are now available (PIDINFO, PIDEXTRACT, PIDTAG, PIDSYNC, etc.)

**Tip:** You can set up AutoCAD to auto-load the plugin on startup by adding it to the Startup Suite.

---

## Feature Testing Checklist

### Feature 1: Enhanced Bidirectional Sync (Phase 2.2)

**Location:** AutoCAD → PIDSYNC command

#### Test 1.1: Drawing to Database Sync
1. [ ] Open AutoCAD with a P&ID drawing
2. [ ] Load the PID Standardization plugin (NETLOAD)
3. [ ] Run command: `PIDSYNC`
4. [ ] Select option 2: "Drawing to Database only"
5. [ ] Verify prompt shows number of equipment found
6. [ ] Confirm sync operation
7. [ ] Check statistics display:
   - [ ] Number added to database
   - [ ] Number updated in database
   - [ ] Processing time shown

**Expected Results:**
- Equipment from drawing appears in database
- Tag numbers match between drawing and database
- No duplicate entries created

#### Test 1.2: Database to Drawing Sync
1. [ ] Open WPF application
2. [ ] Modify some equipment tags in the Equipment tab
3. [ ] Save changes
4. [ ] In AutoCAD, run: `PIDSYNC`
5. [ ] Select option 3: "Database to Drawing only"
6. [ ] Verify equipment blocks in drawing updated with new tags

**Expected Results:**
- Block attributes updated with database values
- Modified dates updated correctly
- No data loss

#### Test 1.3: Full Bidirectional Sync
1. [ ] Make changes in both AutoCAD drawing AND database
2. [ ] Run: `PIDSYNC`
3. [ ] Select option 1: "Full Sync"
4. [ ] Verify both directions synchronized correctly

**Expected Results:**
- Drawing changes reflected in database
- Database changes reflected in drawing
- Conflict resolution handled appropriately

**Issues Found:** ___________________________

---

### Feature 2: Audit Trail (Phase 2.3)

**Location:** WPF Application → Audit Log tab

#### Test 2.1: View Audit Logs
1. [ ] Open WPF application
2. [ ] Navigate to "Audit Log" tab
3. [ ] Select your project from dropdown
4. [ ] Verify audit logs displayed in grid

**Expected Results:**
- Audit logs load without errors
- Columns show: Timestamp, Entity Type, Action, Performed By, Change Details, Source
- Most recent entries shown first

#### Test 2.2: Filter by Entity Type
1. [ ] In Audit Log tab, select "Entity Type" filter
2. [ ] Choose "Equipment"
3. [ ] Verify only equipment-related logs shown
4. [ ] Try other entity types: Line, Instrument, Drawing

**Expected Results:**
- Filtering works immediately
- Count of entries updates correctly
- Grid refreshes properly

#### Test 2.3: Filter by Action
1. [ ] Select "Action" filter
2. [ ] Choose "Created"
3. [ ] Verify only creation events shown
4. [ ] Try: Updated, Deleted, Batch Tagged, Synchronized

**Expected Results:**
- Only selected action types displayed
- Filters combine correctly (Entity Type + Action)

#### Test 2.4: Filter by Time Range
1. [ ] Select "Last" filter
2. [ ] Choose "24 Hours"
3. [ ] Verify only recent entries shown
4. [ ] Try: 7 Days, 30 Days, All Time

**Expected Results:**
- Time filtering accurate
- Older entries excluded for short ranges
- "All Time" shows everything

#### Test 2.5: Create New Audit Entry
1. [ ] Go to Equipment tab
2. [ ] Add new equipment
3. [ ] Return to Audit Log tab
4. [ ] Click "Refresh"
5. [ ] Verify new "Created" entry appears
6. [ ] Check details: performer name, timestamp, change details

**Expected Results:**
- New entry appears at top of list
- Performed By shows your username
- Change Details describes the creation
- Source shows machine name

**Issues Found:** ___________________________

---

### Feature 3: Tag Renumbering Wizard (Phase 2.4)

**Location:** Equipment menu → Tag Renumbering Wizard

#### Test 3.1: Open Wizard
1. [ ] In WPF app, select a project
2. [ ] Click: Equipment → Tag Renumbering Wizard
3. [ ] Verify wizard opens with equipment list

**Expected Results:**
- Dialog opens successfully
- Equipment grid populated
- Filter dropdowns populated
- Summary shows total count

#### Test 3.2: Filter Equipment
1. [ ] Select "Equipment Type" filter (e.g., "Pump")
2. [ ] Verify grid shows only pumps
3. [ ] Select "Area" filter (e.g., "A01")
4. [ ] Verify combined filtering works
5. [ ] Test "Current Tag Pattern" filter (e.g., "P-*")

**Expected Results:**
- Filters work independently and combined
- Equipment count updates correctly
- Grid refreshes immediately

#### Test 3.3: Define Renumbering Pattern
1. [ ] In "Pattern" field, enter: `P-{SEQ:001}`
2. [ ] Verify example shows: "Example: P-001"
3. [ ] Set "Start Number": 10
4. [ ] Set "Increment": 1
5. [ ] Try pattern: `{AREA}-{TYPE}-{SEQ:000}`
6. [ ] Verify example shows correctly

**Expected Results:**
- Pattern preview updates in real-time
- Example shows expected format
- Invalid patterns show error

#### Test 3.4: Preview Changes
1. [ ] Click "Select All"
2. [ ] Click "Preview Changes"
3. [ ] Verify "New Tag (Preview)" column filled
4. [ ] Check sequence numbering correct
5. [ ] Verify no duplicates in preview

**Expected Results:**
- New tags generated correctly
- Sequence increments properly
- Preview column highlighted

#### Test 3.5: Apply Renumbering
1. [ ] Review preview
2. [ ] Click "Apply Renumbering"
3. [ ] Read confirmation dialog
4. [ ] Click "Yes"
5. [ ] Wait for completion message
6. [ ] Verify success count shown

**Expected Results:**
- Transaction completes successfully
- Success message shows count
- Equipment tab updates with new tags
- Audit log entries created

#### Test 3.6: Duplicate Detection
1. [ ] Create pattern that generates existing tag
2. [ ] Click "Preview Changes"
3. [ ] Click "Apply Renumbering"
4. [ ] Verify warning about conflicts

**Expected Results:**
- Conflict warning dialog appears
- List of conflicting tags shown
- Option to cancel operation

#### Test 3.7: Verify Audit Trail
1. [ ] After renumbering, go to Audit Log tab
2. [ ] Filter by Action: "Updated"
3. [ ] Verify entries for renamed equipment
4. [ ] Check OldValues and NewValues in details

**Expected Results:**
- One audit entry per renamed equipment
- Change Details mentions "Tag Renumbering Wizard"
- Old and new tag numbers recorded

**Issues Found:** ___________________________

---

### Feature 4: Hierarchical View (Phase 3.1)

**Location:** Equipment menu → Hierarchical View

#### Test 4.1: Open Hierarchical View
1. [ ] Select a project with equipment data
2. [ ] Click: Equipment → Hierarchical View
3. [ ] Verify dialog opens
4. [ ] Check tree view populated

**Expected Results:**
- Dialog opens full screen
- Tree view shows hierarchy
- Details panel on right side
- Status bar shows "Ready"

#### Test 4.2: Group by Area View
1. [ ] Ensure "Group by Area" radio selected (default)
2. [ ] Expand an area node
3. [ ] Verify equipment types grouped under area
4. [ ] Expand equipment type
5. [ ] Verify individual equipment listed

**Expected Results:**
- Areas show count of items
- Equipment types grouped correctly
- Individual equipment accessible
- Icons displayed correctly (📍 for area, ⚙ for type, • for equipment)

#### Test 4.3: Group by Equipment Type View
1. [ ] Select "Group by Equipment Type" radio
2. [ ] Verify tree rebuilds
3. [ ] Expand an equipment type (e.g., "Pump")
4. [ ] Verify all pumps listed

**Expected Results:**
- Tree reorganizes by type
- All equipment of same type together
- Count indicators correct
- Alphabetical ordering

#### Test 4.4: Process Flow View
1. [ ] Select "Process Flow (Connections)" radio
2. [ ] Verify tree shows connection hierarchy
3. [ ] Expand root equipment
4. [ ] Trace downstream connections

**Expected Results:**
- Root equipment (no upstream) at top level
- Downstream equipment as children
- Process flow visualized
- Circular references detected with ⚠ icon

#### Test 4.5: Group by Drawing View
1. [ ] Select "Group by Drawing" radio
2. [ ] Verify drawings listed
3. [ ] Expand a drawing
4. [ ] Verify equipment from that drawing shown

**Expected Results:**
- Drawings with equipment shown
- Equipment grouped by source drawing
- Unassigned equipment in separate node
- Drawing icons (📄) displayed

#### Test 4.6: Equipment Details Display
1. [ ] Click on any equipment in tree
2. [ ] Verify details panel updates
3. [ ] Check all fields populated:
   - [ ] Tag Number
   - [ ] Equipment Type
   - [ ] Description
   - [ ] Service, Area, Status
   - [ ] Manufacturer, Model
   - [ ] Drawing reference

**Expected Results:**
- Details load immediately
- All fields accurate
- Process parameters shown correctly
- No errors in status bar

#### Test 4.7: Connected Lines Tab
1. [ ] Select equipment with connected lines
2. [ ] Click "Connected Lines" tab
3. [ ] Verify incoming and outgoing lines shown
4. [ ] Check Direction column

**Expected Results:**
- Lines associated with equipment shown
- Direction ("Incoming"/"Outgoing") correct
- Line details accurate (number, service, size)

#### Test 4.8: Connected Equipment Tab
1. [ ] Select equipment with connections
2. [ ] Click "Connected Equipment" tab
3. [ ] Verify upstream/downstream equipment shown
4. [ ] Check Relationship column

**Expected Results:**
- Connected equipment listed
- Relationship ("Upstream"/"Downstream") correct
- Equipment details shown

#### Test 4.9: Instruments Tab
1. [ ] Select equipment with instruments
2. [ ] Click "Instruments" tab
3. [ ] Verify instruments displayed
4. [ ] Check range and units

**Expected Results:**
- All associated instruments shown
- Instrument details accurate
- Range formatting correct

#### Test 4.10: Search Functionality
1. [ ] Enter tag number in search box
2. [ ] Verify tree filters (when implemented)
3. [ ] Click "Clear" button
4. [ ] Verify tree resets

**Expected Results:**
- Search filters tree view
- Clear button works
- Status bar updates

**Issues Found:** ___________________________

---

## Integration Testing

### Integration Test 1: Complete Workflow
Test the entire workflow from drawing to reporting:

1. [ ] **Extract from Drawing** (AutoCAD)
   - Run PIDEXTRACT
   - Review extracted equipment

2. [ ] **Tag Equipment** (AutoCAD)
   - Run PIDTAG
   - Assign tags using wizard

3. [ ] **Sync to Database** (AutoCAD)
   - Run PIDSYNC → Drawing to Database
   - Verify equipment in WPF app

4. [ ] **Renumber Tags** (WPF)
   - Use Tag Renumbering Wizard
   - Apply new pattern

5. [ ] **Sync to Drawing** (AutoCAD)
   - Run PIDSYNC → Database to Drawing
   - Verify blocks updated

6. [ ] **Review Changes** (WPF)
   - Check Audit Log
   - Verify all operations logged

7. [ ] **Explore Hierarchy** (WPF)
   - Open Hierarchical View
   - Review relationships

**Expected Results:**
- Complete workflow executes smoothly
- Data consistency maintained
- All changes audited
- No data loss or corruption

### Integration Test 2: Multi-User Scenario
If testing with multiple users:

1. [ ] User A creates equipment in WPF
2. [ ] User B syncs from database to drawing
3. [ ] User B modifies in AutoCAD
4. [ ] User A syncs from drawing to database
5. [ ] Check audit log shows both users

**Expected Results:**
- Concurrent operations work
- Audit log tracks both users
- No data conflicts

---

## Performance Testing

### Performance Test 1: Large Dataset
Test with substantial data:

1. [ ] Import 100+ equipment items
2. [ ] Run PIDSYNC (Full)
3. [ ] Record time taken
4. [ ] Open Hierarchical View
5. [ ] Test responsiveness

**Benchmarks:**
- [ ] Sync: < 30 seconds for 100 items
- [ ] Hierarchical View: Opens in < 5 seconds
- [ ] Audit Log: Filters apply in < 2 seconds

### Performance Test 2: Audit Log Query
1. [ ] Generate 1000+ audit entries (batch operations)
2. [ ] Filter by date range: 24 hours
3. [ ] Record query time
4. [ ] Try "All Time" filter

**Benchmarks:**
- [ ] Filtered queries: < 1 second
- [ ] Full data load: < 5 seconds

---

## Error Handling Testing

### Error Test 1: Database Connection
1. [ ] Stop SQL Server
2. [ ] Try to open WPF app
3. [ ] Verify error message displayed
4. [ ] Restart SQL Server
5. [ ] Retry operation

**Expected Results:**
- Clear error message
- Application doesn't crash
- Retry works after reconnection

### Error Test 2: Invalid Pattern
1. [ ] Open Tag Renumbering Wizard
2. [ ] Enter invalid pattern: `{INVALID}`
3. [ ] Click Preview
4. [ ] Verify error handling

**Expected Results:**
- Validation error shown
- Clear message about issue
- Application remains stable

### Error Test 3: Circular References
1. [ ] Create circular equipment connections (A→B→A)
2. [ ] Open Hierarchical View → Process Flow
3. [ ] Verify circular reference detection

**Expected Results:**
- Circular reference detected
- Warning icon (⚠) shown
- No infinite loop or crash

---

## Regression Testing

Verify existing features still work:

### Regression Test 1: Basic Equipment CRUD
1. [ ] Add equipment via WPF
2. [ ] Edit equipment details
3. [ ] Delete equipment
4. [ ] Verify all operations work

**Expected Results:**
- All CRUD operations functional
- No new bugs introduced

### Regression Test 2: Excel Import/Export
1. [ ] Export equipment to Excel
2. [ ] Modify Excel file
3. [ ] Import back to database
4. [ ] Verify data integrity

**Expected Results:**
- Import/export unchanged
- Data formats compatible

### Regression Test 3: AutoCAD Commands
Test all existing commands still work:
1. [ ] PIDINFO - Show plugin info
2. [ ] PIDEXTRACT - Extract equipment
3. [ ] PIDTAG - Tag single equipment
4. [ ] PIDTAGALL - Batch tag
5. [ ] PIDSYNC - Sync (already tested above)

**Expected Results:**
- All commands functional
- No regression in behavior

---

## Known Limitations

Document any limitations discovered:

1. **Search in Hierarchical View**: Basic implementation, may need enhancement
2. **Audit Log Export**: Not yet implemented (future feature)
3. **Pattern Templates**: Not saved/loaded (future feature)

---

## Bug Report Template

If you find issues, document them:

```
BUG REPORT
==========
Feature: [Feature name]
Test Case: [Which test]
Steps to Reproduce:
1.
2.
3.

Expected Result:
[What should happen]

Actual Result:
[What actually happened]

Error Message:
[Any error messages]

Screenshot/Logs:
[Attach if available]

Environment:
- Windows Version:
- AutoCAD Version:
- .NET Version:
- SQL Server Version:
```

---

## Sign-Off Checklist

Once all tests pass, sign off:

- [ ] All feature tests completed
- [ ] All integration tests passed
- [ ] Performance acceptable
- [ ] Error handling verified
- [ ] Regression tests passed
- [ ] Database migration successful
- [ ] Documentation reviewed
- [ ] User training materials prepared
- [ ] Production deployment approved

**Tester Name:** ___________________________

**Date:** ___________________________

**Signature:** ___________________________

---

## Support

For issues or questions:
1. Check [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) for feature details
2. Review [MIGRATION_README.md](MIGRATION_README.md) for database setup
3. Check commit history for specific changes
4. Contact development team

---

**Version:** 1.0.4
**Last Updated:** 2026-01-18
**Created by:** Claude Sonnet 4.5
