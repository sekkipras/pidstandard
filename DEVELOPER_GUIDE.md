# Developer Guide - P&ID Standardization Application

## Quick Start for Future Development

### Setup Your Development Environment

1. **Clone the repository** (once you push to GitHub):
   ```bash
   git clone https://github.com/your-org/PIDStandardization.git
   cd PIDStandardization
   ```

2. **Open in Visual Studio 2022**:
   - Double-click `PIDStandardization.sln`
   - Visual Studio will restore NuGet packages automatically

3. **Set up the database**:
   - Ensure SQL Server is running
   - Run the application once - database will be created automatically
   - Or manually run migrations (see below)

4. **Run the application**:
   - Set `PIDStandardization.UI` as startup project
   - Press F5 to debug

---

## Making Changes to the Code

### Workflow for Changes

```bash
# 1. Create a new branch for your feature
git checkout -b feature/add-excel-export

# 2. Make your changes in Visual Studio
# Edit files, add features, fix bugs

# 3. Test your changes
# Run the application, test thoroughly

# 4. Commit your changes
git add .
git commit -m "Add Excel export functionality"

# 5. Push to GitHub
git push origin feature/add-excel-export

# 6. Create a Pull Request on GitHub
# Review and merge when ready
```

### Common Development Tasks

#### Adding a New Feature

1. **Create new files** in appropriate project:
   - UI code → `PIDStandardization.UI`
   - Business logic → `PIDStandardization.Services`
   - Database entities → `PIDStandardization.Core/Entities`
   - AutoCAD features → `PIDStandardization.AutoCAD`

2. **Test locally** by running from Visual Studio

3. **Build and deploy** (see below)

#### Modifying Database Schema

1. **Edit entity classes** in `PIDStandardization.Core/Entities`

2. **Create migration**:
   ```bash
   dotnet ef migrations add YourMigrationName --project PIDStandardization/PIDStandardization.Data --startup-project PIDStandardization/PIDStandardization.UI
   ```

3. **Update database**:
   ```bash
   dotnet ef database update --project PIDStandardization/PIDStandardization.Data --startup-project PIDStandardization/PIDStandardization.UI
   ```

4. **Test migration** on a clean database before deploying

#### Fixing a Bug

1. **Create a bugfix branch**:
   ```bash
   git checkout -b bugfix/fix-equipment-save-error
   ```

2. **Reproduce the bug** in your development environment

3. **Fix the issue** in the appropriate file

4. **Test the fix** thoroughly

5. **Commit and push**:
   ```bash
   git add .
   git commit -m "Fix: Equipment save error when tag is empty"
   git push origin bugfix/fix-equipment-save-error
   ```

---

## Building and Deploying

### Option 1: Automated Build (Recommended)

**Just run the automated script:**

```bash
BUILD_AND_DEPLOY.bat
```

This script will:
1. Clean previous builds
2. Build the solution
3. Publish the application
4. Copy files to Deployment_Package
5. Create the installer (if Inno Setup is installed)

**Result**: `Deployment_Package/Installer_Output/PIDStandardization_Setup_v1.0.0.exe`

### Option 2: Manual Build

**If you want more control:**

1. **Build in Visual Studio**:
   - Build → Build Solution (or Ctrl+Shift+B)
   - Select "Release" configuration

2. **Publish the UI**:
   ```bash
   dotnet publish PIDStandardization/PIDStandardization.UI/PIDStandardization.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Published
   ```

3. **Copy to Deployment_Package**:
   - Copy `Published/PIDStandardization.UI.exe` to `Deployment_Package/`
   - Copy `PIDStandardization.AutoCAD/bin/Release/` to `Deployment_Package/AutoCAD_Plugin/`

4. **Create installer**:
   - Open `Deployment_Package/PIDStandardization_Installer.iss` in Inno Setup
   - Press F9 to compile

---

## Version Management

### Before Creating a New Release

1. **Update version number** in these files:

   **File 1**: `Deployment_Package/PIDStandardization_Installer.iss`
   ```pascal
   #define MyAppVersion "1.1.0"  // Line 7 - Change version here
   ```

   **File 2**: `PIDStandardization.UI/MainWindow.xaml.cs`
   ```csharp
   // In About_Click method (around line 102)
   "Version 1.1.0\n\n"  // Change version here
   ```

2. **Update** `README.md` version history section

3. **Commit version changes**:
   ```bash
   git add .
   git commit -m "Bump version to 1.1.0"
   git tag v1.1.0
   git push origin main --tags
   ```

### Creating a GitHub Release

1. **Build the installer** using `BUILD_AND_DEPLOY.bat`

2. **Go to GitHub** → Releases → "Create new release"

3. **Fill in details**:
   - Tag: `v1.1.0`
   - Title: `P&ID Standardization v1.1.0`
   - Description: List of changes and features

4. **Upload** `PIDStandardization_Setup_v1.1.0.exe`

5. **Publish release**

---

## Project Architecture

### Solution Structure

```
PIDStandardization.sln
├── PIDStandardization.Core          # Domain entities, interfaces, enums
├── PIDStandardization.Data          # EF Core, DbContext, migrations
├── PIDStandardization.Services      # Business logic, tagging services
├── PIDStandardization.UI            # WPF application, views, dialogs
├── PIDStandardization.AutoCAD       # AutoCAD plugin, commands
└── PIDStandardization.Tests         # Unit tests (future)
```

### Key Files to Know

| File | Purpose |
|------|---------|
| `PIDStandardization.Core/Entities/Equipment.cs` | Equipment entity definition |
| `PIDStandardization.Data/Context/PIDDbContext.cs` | Database context and configuration |
| `PIDStandardization.UI/MainWindow.xaml` | Main application UI |
| `PIDStandardization.AutoCAD/Commands/PIDCommands.cs` | AutoCAD commands (PIDEXTRACTDB, etc.) |
| `Deployment_Package/PIDStandardization_Installer.iss` | Installer configuration |

### Adding Dependencies

**NuGet packages** are managed per project:

```bash
# Add package to UI project
dotnet add PIDStandardization/PIDStandardization.UI package PackageName

# Add package to AutoCAD project
dotnet add PIDStandardization/PIDStandardization.AutoCAD package PackageName
```

---

## Testing

### Manual Testing Checklist

Before deploying a new version:

- [ ] Create new project (both Custom and KKS modes)
- [ ] Import a drawing file
- [ ] Extract equipment using PIDEXTRACTDB in AutoCAD
- [ ] View equipment list
- [ ] Add/edit equipment manually
- [ ] Delete a drawing
- [ ] Test welcome screen
- [ ] Test all menu items
- [ ] Verify database migrations work

### Automated Testing (Future)

Unit tests go in `PIDStandardization.Tests` project:

```bash
dotnet test
```

---

## Troubleshooting Development Issues

### "Could not load assembly" errors

**Solution**: Clean and rebuild
```bash
dotnet clean
dotnet build
```

### Database migration fails

**Solution**: Drop database and recreate
```bash
dotnet ef database drop --project PIDStandardization/PIDStandardization.Data --startup-project PIDStandardization/PIDStandardization.UI
dotnet ef database update --project PIDStandardization/PIDStandardization.Data --startup-project PIDStandardization/PIDStandardization.UI
```

### AutoCAD plugin won't load

**Solution**: Check for missing DLLs
- Ensure `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` in .csproj
- Rebuild in Release mode
- Check bin/Release folder has all EF Core DLLs

### Installer compilation fails

**Solution**: Check file paths in .iss file match actual files in Deployment_Package

---

## Git Best Practices

### Branch Naming

- Features: `feature/description`
- Bugfixes: `bugfix/description`
- Hotfixes: `hotfix/description`

### Commit Messages

Good commit messages:
```
Add Excel export functionality for equipment lists
Fix database connection string for production
Update welcome screen with new features
```

Bad commit messages:
```
changes
fix
update
```

### Before Pushing

Always:
1. Pull latest changes: `git pull origin main`
2. Test your changes
3. Review what you're committing: `git diff`

---

## Resources

- **Entity Framework Core Docs**: https://docs.microsoft.com/ef/core/
- **WPF Documentation**: https://docs.microsoft.com/dotnet/desktop/wpf/
- **AutoCAD .NET API**: https://help.autodesk.com/view/OARX/2026/ENU/
- **Inno Setup Help**: https://jrsoftware.org/ishelp/

---

## Getting Help

- Check this guide first
- Review existing code for examples
- Create an issue on GitHub
- Contact the development team

---

## Future Enhancements Roadmap

1. **Excel Import/Export** (Priority 1)
2. **Advanced Validation Rules** (Priority 2)
3. **Reports Generation** (Priority 2)
4. **Multi-user Support** (Future)
5. **Cloud Backup** (Future)
