# P&ID Standardization Application

A comprehensive Windows desktop application for managing P&ID equipment tagging and standardization with AutoCAD integration.

## Features

- **Dual Tagging Modes**: Support for Custom and KKS (DIN 40719) tagging standards
- **AutoCAD Integration**: Extract equipment directly from P&ID drawings (AutoCAD 2026)
- **Smart Block Learning**: System learns equipment types from AutoCAD block names
- **Drawing Management**: Version control and centralized storage for P&ID drawings
- **Database Management**: SQL Server backend for equipment tracking
- **Excel Import/Export**: Seamless data exchange (coming soon)
- **Welcome Screen**: Built-in quick start guide for new users

## System Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in installer)
- SQL Server 2022 Express or higher
- AutoCAD 2026 (optional, for equipment extraction features)
- 200 MB free disk space

## Installation

### For End Users

1. Download `PIDStandardization_Setup_v1.0.0.exe` from Releases
2. Run the installer
3. Follow the installation wizard
4. Launch the application from Start Menu or Desktop

### Database Configuration

The application uses a shared configuration file (`appsettings.json`) for database connectivity.

**Default Location**: `C:\Program Files\PIDStandardization\appsettings.json`

**To change database connection**:
1. Close the application
2. Edit `appsettings.json` with your SQL Server details
3. Restart the application

For detailed configuration instructions, see [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md)

### For Developers

1. Clone this repository
2. Open `PIDStandardization.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution
5. Run `PIDStandardization.UI` project

## Project Structure

```
PIDStandardization/
├── PIDStandardization.Core/          # Domain entities and interfaces
├── PIDStandardization.Data/          # Entity Framework DbContext and migrations
├── PIDStandardization.Services/      # Business logic and services
├── PIDStandardization.UI/            # WPF user interface
├── PIDStandardization.AutoCAD/       # AutoCAD 2026 plugin
├── PIDStandardization.Tests/         # Unit tests
└── Deployment_Package/               # Installer and deployment files
```

## Development Workflow

### Making Changes

1. Create a new branch for your feature/fix:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Make your changes in Visual Studio

3. Test your changes thoroughly

4. Commit your changes:
   ```bash
   git add .
   git commit -m "Description of your changes"
   ```

5. Push to GitHub:
   ```bash
   git push origin feature/your-feature-name
   ```

### Creating a New Release

1. Update version number in:
   - `Deployment_Package/PIDStandardization_Installer.iss` (line 7)
   - `PIDStandardization.UI/MainWindow.xaml.cs` (About dialog, line 102)

2. Build the solution in Release mode:
   ```bash
   dotnet publish PIDStandardization/PIDStandardization.UI/PIDStandardization.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Published
   ```

3. Copy published files to Deployment_Package:
   ```bash
   # See BUILD_AND_DEPLOY.bat for automated script
   ```

4. Compile installer with Inno Setup:
   - Open `PIDStandardization_Installer.iss`
   - Press F9 to compile
   - Installer created in `Deployment_Package/Installer_Output/`

5. Create GitHub release and upload the installer

## AutoCAD Plugin Commands

- `PIDEXTRACTDB` - Extract all equipment from drawing and save to database
- `PIDTAG` - Tag individual equipment blocks with tag numbers (fully functional)
- `PIDEXTRACT` - View equipment in drawing (preview mode, no database save)
- `PIDINFO` - Show plugin information and command list

**Note**: `PIDTAG` is now fully implemented! Tag blocks one-by-one with auto-generation, link to existing equipment, or enter custom tags.

## Database Migrations

When making database schema changes:

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project PIDStandardization.Data --startup-project PIDStandardization.UI

# Update database
dotnet ef database update --project PIDStandardization.Data --startup-project PIDStandardization.UI
```

## Technologies Used

- **.NET 8.0** - Application framework
- **WPF** - User interface
- **Entity Framework Core 8.0** - ORM
- **SQL Server 2022** - Database
- **AutoCAD .NET API** - CAD integration
- **Inno Setup** - Installer creation

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

© 2026 - All rights reserved

## Support

For issues or questions:
- Check the [Installation Guide](Deployment_Package/INSTALLATION_GUIDE.txt)
- Contact the development team
- Create an issue on GitHub

## Version History

### Version 1.1.0 (Current)
- Added configurable database connection via `appsettings.json`
- Shared configuration between WPF app and AutoCAD plugin
- Lazy loading enabled for navigation properties
- Fixed critical code quality issues
- Comprehensive configuration guide added

### Version 1.0.0
- Initial release
- Equipment, Lines, and Instruments management
- AutoCAD 2026 integration
- Drawing assignment functionality
- User guide and documentation
- Project and equipment management
- AutoCAD integration
- Drawing version control
- Block learning system
- Welcome screen with quick start guide
