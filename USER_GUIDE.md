# P&ID Standardization Application - User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Project Management](#project-management)
3. [Equipment Management](#equipment-management)
4. [Lines Management](#lines-management)
5. [Instruments Management](#instruments-management)
6. [Tag Validation](#tag-validation)
7. [AutoCAD Integration](#autocad-integration)
8. [Excel Export](#excel-export)
9. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Installation
1. Download `PIDStandardization.UI.exe` from the GitHub releases or build artifacts
2. Extract the file to a desired location
3. Run `PIDStandardization.UI.exe` (no .NET installation required - it's self-contained)

### First Launch
On first launch, you'll see a welcome screen explaining the key features:
- **Dual Tagging Modes**: ISA and Custom tagging standards
- **AutoCAD Integration**: Extract equipment from P&ID drawings
- **Smart Block Learning**: AI-powered block type recognition
- **Excel Import/Export**: Bulk operations support
- **Version Control**: Track drawing versions and equipment changes

Click **Get Started** to begin.

---

## Project Management

### Creating a New Project
1. Click **Create New Project** on the main window
2. Fill in the project details:
   - **Project Name**: Required (e.g., "Refinery Unit 100")
   - **Project Number**: Optional (e.g., "PRJ-2025-001")
   - **Client**: Optional company/client name
   - **Tagging Mode**: Select either **ISA** or **Custom**
     - **ISA Mode**: Industry standard instrument tagging (e.g., PT-101, FT-202)
     - **Custom Mode**: Flexible tagging for equipment (e.g., P-101, TK-201)

3. Click **Create Project**

### Opening an Existing Project
1. Click **Open Project** from the main window
2. Select the project from the list
3. Click **Open**

---

## Equipment Management

### Adding Equipment

#### Manual Entry
1. Navigate to the **Equipment** tab
2. Click **Add Equipment**
3. Fill in the equipment details:

**Basic Information:**
- **Tag Number**: Required (e.g., P-101, TK-201)
  - Click **Auto Generate** to create a tag based on equipment type
  - Click **Validate Tag** to check if it follows project standards
- **Equipment Type**: Select from dropdown (Pump, Valve, Tank, Heat Exchanger, etc.)
- **Description**: Optional text description
- **Service**: What the equipment is used for
- **Area/Location**: Physical location in the plant
- **Status**: Planned, Installed, Commissioned, or Decommissioned
- **Manufacturer** and **Model**: Optional vendor information

**Equipment Connectivity** (Optional - useful for valves):
- **Upstream Equipment**: Select equipment feeding into this equipment
- **Downstream Equipment**: Select equipment receiving flow from this equipment

**Process Parameters** (Optional):
All process parameters support multiple units via dropdown:

*Operating Conditions:*
- **Operating Pressure**: Value + Unit (bar, psi, kPa, MPa)
- **Operating Temperature**: Value + Unit (°C, °F, K)
- **Flow Rate**: Value + Unit (m³/h, L/min, gpm, kg/h)

*Design Conditions:*
- **Design Pressure**: Maximum pressure rating
- **Design Temperature**: Maximum temperature rating
- **Power/Capacity**: Value + Unit (kW, HP, m³, L, tons)

4. Click **Save**

#### From AutoCAD Drawing
1. Open AutoCAD with the P&ID drawing
2. Run the `PIDEXTRACTDB` command in AutoCAD
3. Select the project and (optionally) the source drawing
4. The equipment will be automatically extracted and added to the database
5. Review and edit the extracted equipment in the Equipment tab

Alternatively, use `PIDTAG` to tag equipment one block at a time as you design

### Editing Equipment
1. Select equipment from the grid
2. Click **Edit Equipment**
3. Modify fields as needed
4. Click **Save**

### Deleting Equipment
1. Select equipment from the grid
2. Click **Delete Equipment**
3. Confirm the deletion

---

## Lines Management

Lines represent piping between equipment.

### Adding a Line
1. Navigate to the **Lines** tab (once implemented in Phase 2)
2. Click **Add Line**
3. Fill in the line details:

**Basic Information:**
- **Line Number**: Required (e.g., L-101, 2"-WS-101)
- **Service**: What flows through the line (e.g., "Cooling Water", "Steam")
- **Fluid Type**: Select from dropdown (Water, Steam, Air, Nitrogen, Oil, Gas, Chemical)

**Size & Material Specification:**
- **Nominal Size**: Select pipe size (1/2", 3/4", 1", 1-1/2", 2", 3", 4", 6", 8", 10", 12", etc.)
- **Material Spec**: Material specification (e.g., "A106 Grade B", "SS316")
- **Pipe Schedule**: SCH 40, SCH 80, SCH 160, etc.

**Design Conditions:**
- **Design Pressure**: Value + Unit (bar, psi, kPa, MPa)
- **Design Temperature**: Value + Unit (°C, °F, K)

**Equipment Connectivity:**
- **From Equipment**: Starting equipment tag
- **To Equipment**: Ending equipment tag

**Insulation** (Optional):
- **Insulation Required**: Check if line needs insulation
- **Insulation Type**: Type of insulation material
- **Insulation Thickness**: Thickness value

**Additional:**
- **Length**: Total line length

4. Click **Save**

---

## Instruments Management

Instruments can be associated with either equipment OR lines.

### Adding an Instrument
1. Navigate to the **Instruments** tab (once implemented in Phase 3)
2. Click **Add Instrument**
3. Fill in the instrument details:

**Basic Information:**
- **Tag Number**: Required (e.g., PT-101, FT-202, TI-305)
- **Instrument Type**: Transmitter, Indicator, Controller, Switch
- **Measurement Type**: Pressure, Temperature, Flow, Level

**Range & Specifications:**
- **Range Min**: Minimum measurement value
- **Range Max**: Maximum measurement value
- **Units**: Measurement units (bar, psi, °C, °F, m³/h, etc.)
- **Accuracy**: Measurement accuracy specification

**Association:**
Select ONE of the following:
- **Associated with Equipment**: Select an equipment tag from the dropdown
- **Installed on Line**: Select a line number from the dropdown

*Example:* Pressure transmitter PT-101 measuring pressure on Line L-100 between pump P-100 and valve V-100

4. Click **Save**

---

## Tag Validation

### ISA Tagging Mode
ISA tags follow this format: `[Measurement][Device]-[Number]`

**First Letter (Measured Variable):**
- P = Pressure
- T = Temperature
- F = Flow
- L = Level
- A = Analysis
- And more...

**Second Letter (Device Type):**
- T = Transmitter
- I = Indicator
- C = Controller
- S = Switch
- And more...

**Examples:**
- `PT-101`: Pressure Transmitter #101
- `FIC-202`: Flow Indicator Controller #202
- `LT-305`: Level Transmitter #305

### Custom Tagging Mode
Custom mode allows flexible tagging for equipment:
- `P-101`: Pump #101
- `TK-201`: Tank #201
- `V-305`: Valve #305
- `HX-401`: Heat Exchanger #401

### Tag Auto-Generation
1. Select the **Equipment Type** first
2. Click **Auto Generate**
3. The system will create the next available tag number for that type
4. Example: If P-101 exists, it will generate P-102

### Tag Validation
1. Enter a tag number
2. Click **Validate Tag**
3. The system will check:
   - Format compliance with project tagging mode
   - Uniqueness within the project
   - Standard naming conventions

4. Results will show:
   - ✅ Valid tag with any warnings
   - ❌ Invalid tag with specific errors

---

## AutoCAD Integration

### Prerequisites
- AutoCAD 2025 or 2026 installed
- P&ID Standardization AutoCAD plugin loaded

### Loading the Plugin
1. In AutoCAD, type `NETLOAD`
2. Browse to `PIDStandardization.AutoCAD.dll`
3. Click **Load**

### Available Commands

#### PIDEXTRACTDB
Extracts all equipment from the current drawing and saves to database.

**Usage:**
1. Type `PIDEXTRACTDB` and press Enter
2. Select a project from the dialog
3. (Optional) Select a source drawing
4. All equipment blocks are automatically extracted
5. Equipment data is saved to the database
6. Open the P&ID Standardization Application to review

**What gets extracted:**
- Block name and position
- TAG or TAGNUMBER attributes (if present)
- All other attributes (description, manufacturer, etc.)
- Layer information
- Drawing reference

#### PIDTAG
Tag individual equipment blocks with tag numbers.

**Usage:**
1. Type `PIDTAG` and press Enter
2. Select a project from the dialog
3. Click on an equipment block in the drawing
4. In the Tag Assignment dialog, choose one of:
   - **Use existing equipment**: Link to equipment already in database
   - **Auto-generate**: Use suggested tag number (e.g., PUMP-001)
   - **Custom tag**: Enter your own tag number
5. Click OK
6. The tag number is:
   - Written to the block's TAG attribute (if it has one)
   - Saved to the database
   - Marked in extended data for tracking

**Use cases:**
- Tag equipment one-by-one as you design
- Link drawing blocks to existing equipment database records
- Quickly assign tags to new equipment

#### PIDSYNC
Synchronize equipment between the drawing and database (bi-directional).

**Usage:**
1. Type `PIDSYNC` and press Enter
2. Select a project from the dialog
3. The plugin analyzes differences between drawing and database:
   - **New in drawing**: Equipment blocks in drawing not yet in database
   - **Missing in drawing**: Database equipment not found in drawing
   - **In both**: Equipment that exists in both locations
4. Choose an action:
   - **Add**: Add new equipment from drawing to database
   - **Info**: View detailed list of differences
   - **Cancel**: Exit without changes

**Features:**
- Automatically detects equipment type using learned block mappings
- Confidence-based suggestions improve over time
- Safe operation - shows analysis before making changes
- Useful for keeping drawing and database in sync during iterative design

**Use cases:**
- After making multiple changes to the drawing, sync to database
- Verify consistency between drawing and database
- Batch-add new equipment discovered in the drawing
- Audit what equipment exists in drawing vs. database

#### PIDEXTRACT
View equipment in the drawing without saving to database.

**Usage:**
1. Type `PIDEXTRACT` and press Enter
2. Equipment list is displayed in command line
3. No database changes are made
4. Useful for previewing what will be extracted

#### PIDINFO
Display plugin information and available commands.

**Usage:**
1. Type `PIDINFO` and press Enter
2. Plugin version and command list is displayed

---

## Block Learning System

The AutoCAD plugin includes an intelligent Block Learning System that improves equipment type detection over time.

### How It Works
1. **Initial Detection**: When you first extract equipment, the plugin uses pattern matching to detect equipment types from block names
   - Example: "PUMP-100" → detected as "Pump"
   - Example: "V-201" → detected as "Valve"

2. **Learning**: Each time equipment is extracted using PIDEXTRACTDB or PIDSYNC, the plugin learns the mapping between block name and equipment type
   - Mappings are stored in: `%AppData%\PIDStandardization\block_mappings.json`
   - Each mapping has a confidence score (0.0 to 1.0)

3. **Improvement**: Over time, the plugin becomes better at detecting your specific block naming conventions
   - Confidence increases with usage count
   - User-confirmed mappings get higher confidence

4. **Automatic Application**: When extracting equipment, the plugin:
   - Checks for learned mappings first
   - If confidence > 0.5, uses the learned equipment type
   - Otherwise, falls back to pattern matching
   - Learns the mapping for future use

### Benefits
- **Faster extraction**: No need to manually classify equipment types
- **Consistency**: Same block names always map to same equipment types
- **Customization**: Adapts to your company's block naming standards
- **Low maintenance**: Fully automatic - no configuration needed

### Viewing Learned Mappings
The block mappings file (`block_mappings.json`) contains:
```json
[
  {
    "BlockName": "PUMP-100",
    "EquipmentType": "Pump",
    "UsageCount": 5,
    "ConfidenceScore": 0.5,
    "IsUserConfirmed": false,
    "FirstUsedDate": "2026-01-17T10:30:00Z",
    "LastUsedDate": "2026-01-17T14:45:00Z"
  }
]
```

### Resetting Learning
To reset learned mappings:
1. Close AutoCAD
2. Navigate to `%AppData%\PIDStandardization\`
3. Delete `block_mappings.json`
4. The plugin will start learning from scratch

---

## Excel Export

### Exporting Equipment Lists
1. Navigate to the **Equipment** tab
2. Click **Export to Excel**
3. Choose save location
4. Excel file is generated with all equipment data including:
   - Tag numbers
   - Equipment types
   - Descriptions
   - Process parameters (pressure, temperature, flow rate)
   - Connectivity information
   - Vendor data

### Exporting Line Lists
1. Navigate to the **Lines** tab
2. Click **Export to Excel**
3. Excel file contains:
   - Line numbers
   - Service descriptions
   - Sizes and specifications
   - From/To equipment
   - Design conditions
   - Insulation details

### Exporting Instrument Lists
1. Navigate to the **Instruments** tab
2. Click **Export to Excel**
3. Excel file contains:
   - Instrument tags
   - Types and measurements
   - Ranges and accuracy
   - Associated equipment or lines
   - Calibration information

---

## Process Parameter Guidelines

### Pressure Parameters
**Operating Pressure**: Normal operating pressure of the equipment
**Design Pressure**: Maximum pressure the equipment is rated for

*Common Units:*
- bar (metric standard)
- psi (imperial standard)
- kPa (SI unit)
- MPa (high pressure systems)

*Example:* Tank TK-101 operates at 5 bar, designed for 10 bar

### Temperature Parameters
**Operating Temperature**: Normal operating temperature
**Design Temperature**: Maximum temperature rating

*Common Units:*
- °C (Celsius - metric)
- °F (Fahrenheit - imperial)
- K (Kelvin - absolute)

*Example:* Heat exchanger HX-201 operates at 80°C, designed for 150°C

### Flow Rate
**Flow Rate**: Volume or mass flow through the equipment

*Common Units:*
- m³/h (cubic meters per hour)
- L/min (liters per minute)
- gpm (gallons per minute)
- kg/h (kilograms per hour - mass flow)

*Example:* Pump P-101 delivers 50 m³/h of cooling water

### Power/Capacity
**Power**: For motors, pumps, compressors (kW, HP)
**Capacity**: For tanks, vessels (m³, L, tons)

*Examples:*
- Motor M-101: 75 kW
- Tank TK-201: 1000 m³ capacity

---

## Equipment Connectivity Workflow

### Example: Simple Process Flow
```
[Tank TK-100] → [Pump P-101] → [Valve V-101] → [Heat Exchanger HX-201] → [Tank TK-200]
```

**Setup:**
1. **Tank TK-100**: No upstream equipment, Downstream = P-101
2. **Pump P-101**: Upstream = TK-100, Downstream = V-101
3. **Valve V-101**: Upstream = P-101, Downstream = HX-201
4. **Heat Exchanger HX-201**: Upstream = V-101, Downstream = TK-200
5. **Tank TK-200**: Upstream = HX-201, No downstream

**Benefits:**
- Flow path tracing
- Impact analysis when equipment changes
- Line connectivity validation

---

## Troubleshooting

### Database Connection Issues
**Problem**: "Cannot connect to database"
**Solution**:
1. Ensure SQL Server is running
2. Check connection string in settings
3. Verify database exists (created on first launch)

### Tag Validation Errors
**Problem**: "Tag format invalid"
**Solution**:
1. Check project tagging mode (ISA vs Custom)
2. Follow the tag format guide for your mode
3. Use Auto Generate to create a valid tag

### AutoCAD Plugin Not Loading
**Problem**: "Could not load PIDStandardization.AutoCAD.dll"
**Solution**:
1. Verify AutoCAD version compatibility (2025 or 2026)
2. Check that all DLLs are in the same folder
3. Run AutoCAD as Administrator
4. Unblock the DLL in Windows properties

### Process Parameters Not Saving
**Problem**: Parameters don't appear after saving
**Solution**:
1. Ensure you selected a unit from the dropdown
2. Check for valid decimal format (use . for decimal point)
3. Verify database migration has been applied

### Missing Equipment in Export
**Problem**: Some equipment missing in Excel export
**Solution**:
1. Check that equipment is marked as Active (IsActive = true)
2. Verify project filter is correct
3. Refresh the equipment grid before exporting

---

## Database Location

The application uses SQL Server Express LocalDB by default.

**Default Connection String:**
```
Server=(localdb)\\mssqllocaldb;Database=PIDStandardization;Trusted_Connection=true
```

**Database Name:** `PIDStandardization`

**Tables:**
- `Projects`: All projects
- `Equipment`: Equipment records with process parameters
- `Lines`: Piping line records
- `Instruments`: Instrument records
- `Drawings`: Drawing metadata
- `ValidationRules`: Tag validation rules
- `BlockMappings`: AutoCAD block type mappings

---

## Keyboard Shortcuts

- `Ctrl+N`: New Project
- `Ctrl+O`: Open Project
- `Ctrl+S`: Save (when in dialog)
- `Ctrl+E`: Export to Excel (when on a tab)
- `Esc`: Cancel/Close dialog

---

## Support

For issues, feature requests, or questions:
- GitHub Issues: https://github.com/sekkipras/pidstandard/issues
- Email: Contact your development team

---

## Version History

**Current Version**: 1.1.0

**Recent Updates:**
- ✅ Added process parameters to equipment (pressure, temperature, flow rate, power/capacity)
- ✅ Added equipment connectivity tracking (upstream/downstream)
- ✅ Enhanced instrument associations (can link to lines or equipment)
- ✅ Unit selection support for all process parameters
- ✅ Optional fields - can be left empty and filled later
- ✅ Improved tag validation system
- ✅ GitHub Actions CI/CD integration

**Coming Soon:**
- Line management UI (Phase 2)
- Instrument management UI (Phase 3)
- Batch import from Excel
- Advanced reporting features
- Drawing comparison tools

---

## Best Practices

1. **Start with Equipment**: Create equipment before lines, as lines connect equipment
2. **Use Tag Validation**: Always validate tags before saving to ensure consistency
3. **Fill Process Parameters**: Add process parameters as they become available for better documentation
4. **Track Connectivity**: Link equipment to show flow paths for easier maintenance
5. **Regular Exports**: Export to Excel regularly for backup and reporting
6. **Version Control**: Use Git to track changes to the database and project files
7. **Consistent Naming**: Follow your organization's naming conventions for services and areas

---

*End of User Guide*
