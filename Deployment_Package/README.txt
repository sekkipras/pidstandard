========================================
P&ID Standardization Application v1.0
Installation and Setup Guide
========================================

CONTENTS OF THIS PACKAGE:
-------------------------
- PIDStandardization.UI.exe       : Main application
- AutoCAD_Plugin/                 : AutoCAD 2026 plugin files
- Templates/                      : Excel templates (coming soon)
- Documentation/                  : User guides (coming soon)

SYSTEM REQUIREMENTS:
-------------------
- Windows 10/11 (64-bit)
- SQL Server 2022 Express or higher
- AutoCAD 2026 (for P&ID extraction features)
- .NET 8.0 Runtime (included in the .exe)

INSTALLATION STEPS:
------------------

1. DATABASE SETUP
   a. Ensure SQL Server 2022 Express is installed and running
   b. The application will automatically create the database on first run
   c. Default connection: Server=(localdb)\mssqllocaldb;Database=PIDStandardization;

2. MAIN APPLICATION SETUP
   a. Copy PIDStandardization.UI.exe to a location of your choice
      (Recommended: C:\Program Files\PIDStandardization\)
   b. Double-click PIDStandardization.UI.exe to launch
   c. The welcome screen will appear on first launch

3. AUTOCAD PLUGIN INSTALLATION
   a. Open AutoCAD 2026
   b. Type: NETLOAD
   c. Browse to: AutoCAD_Plugin\PIDStandardization.AutoCAD.dll
   d. Click Load
   e. The plugin is now loaded and ready to use

   Available AutoCAD Commands:
   - PIDEXTRACTDB : Extract equipment from drawing and save to database
   - PIDEXTRACT   : Extract equipment from drawing (view only)
   - PIDTAG       : Tag equipment in the drawing
   - PIDINFO      : Show plugin information

4. FIRST TIME USE
   a. Launch PIDStandardization.UI.exe
   b. Read the Welcome Screen for Quick Start Guide
   c. Create a new project (File > New Project)
   d. Choose your tagging mode (Custom or KKS)
   e. Import your P&ID drawing (Drawings tab > Import Drawing)
   f. In AutoCAD, run PIDEXTRACTDB to extract equipment

FEATURES:
---------
✓ Dual Tagging Modes (Custom and KKS/DIN 40719)
✓ AutoCAD Integration for equipment extraction
✓ Smart block learning mechanism
✓ Drawing version control and management
✓ Equipment database management
✓ Excel export capabilities (coming soon)

SUPPORT:
--------
For issues or questions, contact the development team.

DATABASE LOCATION:
-----------------
The SQLite database will be created at:
%LOCALAPPDATA%\PIDStandardization\

DRAWING STORAGE:
---------------
Imported drawings are stored at:
%PROGRAMDATA%\PIDStandardization\Drawings\{ProjectName}\

BLOCK MAPPINGS:
--------------
Block learning data is stored at:
%APPDATA%\PIDStandardization\block_mappings.json

TROUBLESHOOTING:
---------------
1. If the application doesn't start:
   - Check if SQL Server is running
   - Run as Administrator

2. If AutoCAD plugin fails to load:
   - Ensure you're using AutoCAD 2026
   - Check that all DLLs in AutoCAD_Plugin folder are present
   - Try running AutoCAD as Administrator

3. If database connection fails:
   - Verify SQL Server instance name in connection string
   - Check Windows Firewall settings

========================================
© 2026 - P&ID Standardization Application
========================================
