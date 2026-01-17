# PIDStandardization Configuration Guide

## Overview

The PIDStandardization application uses a shared configuration file (`appsettings.json`) that controls database connectivity for both the WPF UI application and the AutoCAD plugin.

**Important**: Both applications must use the same configuration file to ensure they connect to the same database.

---

## Configuration File Location

### Default Locations

**WPF Application**:
```
C:\Program Files\PIDStandardization\appsettings.json
```

**AutoCAD Plugin**:
```
C:\Program Files\PIDStandardization\AutoCAD_Plugin\appsettings.json
```

### Development Locations

**Build Output Directories**:
- `PIDStandardization.Data\bin\Release\net8.0\appsettings.json`
- `PIDStandardization.UI\bin\Release\net8.0-windows\appsettings.json`
- `PIDStandardization.AutoCAD\bin\Release\net8.0-windows\appsettings.json`

---

## Configuration File Structure

```json
{
  "DatabaseSettings": {
    "ConnectionString": "Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;",
    "EnableSensitiveDataLogging": false,
    "CommandTimeout": 30
  }
}
```

### Configuration Parameters

#### ConnectionString
The SQL Server connection string used by Entity Framework Core.

**Format**: Standard ADO.NET connection string

**Default**: `Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;`

**Common Scenarios**:

1. **Local SQL Server Express (Default)**
   ```
   Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;
   ```

2. **Local SQL Server (Full Edition)**
   ```
   Server=localhost;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;
   ```

3. **SQL Server LocalDB**
   ```
   Server=(localdb)\\MSSQLLocalDB;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;
   ```

4. **Network SQL Server with Windows Authentication**
   ```
   Server=SERVERNAME\\INSTANCENAME;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;
   ```

5. **Network SQL Server with SQL Authentication**
   ```
   Server=SERVERNAME;Database=PIDStandardization;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
   ```

6. **Custom Database Name**
   ```
   Server=localhost\\SQLEXPRESS;Database=PIDStandardization_Project1;Trusted_Connection=True;TrustServerCertificate=True;
   ```

7. **Azure SQL Database**
   ```
   Server=yourserver.database.windows.net,1433;Database=PIDStandardization;User Id=yourusername;Password=yourpassword;Encrypt=True;
   ```

#### EnableSensitiveDataLogging
Controls whether Entity Framework Core logs SQL queries with parameter values.

**Type**: Boolean (true/false)

**Default**: false

**Use Cases**:
- Set to `true` during development/debugging to see SQL queries in logs
- **Must be `false` in production** for security and performance

#### CommandTimeout
SQL command timeout in seconds.

**Type**: Integer

**Default**: 30

**Recommended Range**: 30-60 seconds

**Use Cases**:
- Increase for slow networks or large data operations
- Decrease for better responsiveness with fast connections

---

## How to Change Configuration

### Step-by-Step Instructions

1. **Close all applications**
   - Close the WPF UI application
   - Close AutoCAD (or unload the plugin with `NETUNLOAD`)

2. **Locate the configuration file**
   - Navigate to the installation directory
   - Find `appsettings.json`

3. **Edit the configuration file**
   - Open with any text editor (Notepad, Notepad++, VS Code)
   - Modify the desired settings
   - **Important**: Ensure JSON syntax is valid (commas, quotes, braces)

4. **Save the file**
   - Save changes
   - Verify file is not read-only

5. **Restart applications**
   - Launch the WPF application - it will use the new settings
   - In AutoCAD, load the plugin with `NETLOAD`

### Example: Changing to Network SQL Server

**Before**:
```json
{
  "DatabaseSettings": {
    "ConnectionString": "Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;",
    "EnableSensitiveDataLogging": false,
    "CommandTimeout": 30
  }
}
```

**After**:
```json
{
  "DatabaseSettings": {
    "ConnectionString": "Server=ENGSERVER01\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;",
    "EnableSensitiveDataLogging": false,
    "CommandTimeout": 60
  }
}
```

---

## Deployment Scenarios

### Scenario 1: Single-User Workstation

**Setup**:
- SQL Server Express installed on workstation
- Both WPF app and AutoCAD plugin on same machine
- Single user

**Configuration**:
```json
"ConnectionString": "Server=localhost\\SQLEXPRESS;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;"
```

**File Management**:
- Keep `appsettings.json` in both application folders
- Contents must be identical
- Or use a symlink/junction to share one file

### Scenario 2: Multi-User Network

**Setup**:
- Centralized SQL Server on network
- Multiple users with WPF app and/or AutoCAD plugin
- Shared database

**Configuration**:
```json
"ConnectionString": "Server=SQLSERVER01;Database=PIDStandardization;Trusted_Connection=True;TrustServerCertificate=True;"
```

**File Distribution**:
- Deploy `appsettings.json` with same connection string to all workstations
- Consider using group policy or deployment scripts
- Users must have network access and SQL Server permissions

### Scenario 3: Cloud Database (Azure SQL)

**Setup**:
- Azure SQL Database
- Users connect over internet
- Enhanced security requirements

**Configuration**:
```json
"ConnectionString": "Server=yourcompany.database.windows.net,1433;Database=PIDStandardization;User Id=appuser;Password=SecurePassword123!;Encrypt=True;"
```

**Security Notes**:
- Use strong passwords
- Consider Azure AD authentication
- Enable firewall rules for user IPs
- Use encryption (Encrypt=True)

### Scenario 4: Per-Project Databases

**Setup**:
- Separate database for each engineering project
- Users switch between projects

**Configuration** (Project A):
```json
"ConnectionString": "Server=localhost\\SQLEXPRESS;Database=PIDStandardization_ProjectA;Trusted_Connection=True;TrustServerCertificate=True;"
```

**Configuration** (Project B):
```json
"ConnectionString": "Server=localhost\\SQLEXPRESS;Database=PIDStandardization_ProjectB;Trusted_Connection=True;TrustServerCertificate=True;"
```

**Workflow**:
- Create separate installation directories per project, OR
- Manually edit `appsettings.json` when switching projects, OR
- Use batch scripts to swap configuration files

---

## Troubleshooting

### Connection Errors

#### Error: "Cannot open database 'PIDStandardization'"

**Cause**: Database doesn't exist

**Solution**:
1. Run the WPF application first - it will create the database automatically
2. Or manually create the database in SQL Server Management Studio
3. Or run Entity Framework migrations: `dotnet ef database update`

#### Error: "A network-related or instance-specific error occurred"

**Causes**:
- SQL Server not running
- Server name incorrect
- Network connectivity issues
- Firewall blocking connection

**Solutions**:
1. Verify SQL Server is running:
   - Open SQL Server Configuration Manager
   - Check SQL Server service status

2. Verify server name:
   - Open SQL Server Management Studio
   - Note the exact server name shown

3. Test connectivity:
   - Try connecting with SQL Server Management Studio using the same connection string

4. Check firewall:
   - Allow SQL Server through Windows Firewall
   - Enable TCP/IP in SQL Server Configuration Manager
   - Default port: 1433

#### Error: "Login failed for user"

**Causes**:
- Incorrect username/password
- User doesn't have database permissions
- Windows Authentication not working

**Solutions**:
1. For Windows Authentication (`Trusted_Connection=True`):
   - Ensure current Windows user has SQL Server login
   - Grant permissions: `USE PIDStandardization; EXEC sp_addrolemember 'db_owner', 'DOMAIN\Username';`

2. For SQL Authentication:
   - Verify username and password are correct
   - Enable SQL Server Authentication (Mixed Mode) in server properties
   - Restart SQL Server after changing authentication mode

### Configuration File Issues

#### Error: Configuration changes not taking effect

**Solutions**:
1. Verify you edited the correct `appsettings.json`:
   - Check file path matches running application
   - Search for multiple copies on disk

2. Restart applications:
   - Close WPF app completely
   - In AutoCAD: Type `NETUNLOAD`, select plugin, then `NETLOAD` again

3. Check file permissions:
   - Ensure file is not read-only
   - Verify user has write access to directory

#### Error: JSON syntax errors

**Symptoms**:
- Application fails to start
- Falls back to default connection string

**Common Mistakes**:
```json
// WRONG - Missing comma
{
  "DatabaseSettings": {
    "ConnectionString": "..."
    "EnableSensitiveDataLogging": false
  }
}

// CORRECT
{
  "DatabaseSettings": {
    "ConnectionString": "...",
    "EnableSensitiveDataLogging": false
  }
}
```

**Solutions**:
1. Use a JSON validator (https://jsonlint.com/)
2. Use a code editor with JSON support (VS Code, Notepad++)
3. Copy from a known-good example

### AutoCAD-Specific Issues

#### Error: AutoCAD plugin connects to different database than WPF app

**Cause**: Configuration files not synchronized

**Solution**:
1. Compare both `appsettings.json` files:
   - WPF app location: `C:\Program Files\PIDStandardization\appsettings.json`
   - AutoCAD plugin location: `C:\Program Files\PIDStandardization\AutoCAD_Plugin\appsettings.json`

2. Ensure both have identical connection strings

3. Unload and reload AutoCAD plugin after changes

#### Error: "Configuration file not found"

**Cause**: `appsettings.json` missing from plugin directory

**Solution**:
1. Copy `appsettings.json` to AutoCAD plugin folder
2. Verify file is in same directory as `PIDStandardization.AutoCAD.dll`
3. Reload plugin

---

## Security Best Practices

### Connection String Security

1. **Never hardcode passwords in configuration files for production**
   - Use Windows Authentication when possible
   - If SQL Authentication required, use encrypted storage or secure vaults

2. **Restrict file permissions**
   ```
   icacls appsettings.json /inheritance:r /grant:r "Administrators:F" "SYSTEM:F" "YourUsername:R"
   ```

3. **Use least-privilege database accounts**
   - Don't use `sa` account
   - Create application-specific SQL users with minimal permissions
   - Grant only necessary permissions (db_datareader, db_datawriter, execute on stored procedures)

4. **Enable encryption for network connections**
   - Use `Encrypt=True` in connection string for network/cloud databases
   - Configure SSL/TLS on SQL Server

### Multi-User Deployments

1. **Centralize configuration management**
   - Store master `appsettings.json` in network share
   - Deploy via group policy or SCCM
   - Version control configuration changes

2. **Use environment-specific configurations**
   - Development: Local databases, logging enabled
   - Production: Network database, logging disabled, higher timeout

3. **Audit configuration access**
   - Track who modifies configuration files
   - Use file system auditing on Windows
   - Backup configuration before changes

---

## Advanced Configuration

### Connection String Parameters Reference

| Parameter | Description | Example |
|-----------|-------------|---------|
| Server | SQL Server instance name | `localhost`, `SERVERNAME\\INSTANCE`, `server.database.windows.net` |
| Database | Database name | `PIDStandardization` |
| Trusted_Connection | Use Windows Authentication | `True` or `False` |
| User Id | SQL Authentication username | `sa`, `appuser` |
| Password | SQL Authentication password | `YourPassword123!` |
| Encrypt | Encrypt connection | `True` or `False` |
| TrustServerCertificate | Trust self-signed certificates | `True` or `False` |
| ConnectTimeout | Connection timeout (seconds) | `30` (default) |
| MultipleActiveResultSets | Enable MARS | `True` or `False` |
| ApplicationName | Application name in SQL logs | `PIDStandardization` |

### Connection Pooling

Connection pooling is enabled by default. To configure:

```json
"ConnectionString": "Server=localhost;Database=PIDStandardization;Trusted_Connection=True;Max Pool Size=100;Min Pool Size=5;Pooling=true;"
```

### Failover and High Availability

For SQL Server Always On configurations:

```json
"ConnectionString": "Server=tcp:PrimaryServer,1433;Failover Partner=SecondaryServer;Database=PIDStandardization;Trusted_Connection=True;"
```

---

## Testing Configuration

### Verify Configuration Loading

1. Enable sensitive data logging temporarily:
   ```json
   "EnableSensitiveDataLogging": true
   ```

2. Run the WPF application

3. Check for startup logs showing connection string being loaded

4. **Remember to disable after testing**:
   ```json
   "EnableSensitiveDataLogging": false
   ```

### Test Database Connectivity

**Using WPF Application**:
1. Launch `PIDStandardization.UI.exe`
2. If connection succeeds, you'll see the main window
3. If connection fails, error message will display with details

**Using SQL Server Management Studio**:
1. Copy connection string from `appsettings.json`
2. Translate to SSMS format:
   - Server name: Value of `Server=` parameter
   - Authentication: Based on `Trusted_Connection` or `User Id/Password`
3. Attempt connection
4. If successful, configuration is correct

**Using AutoCAD Plugin**:
1. Load plugin with `NETLOAD`
2. Run `PIDINFO` command
3. Run `PIDEXTRACTDB` command
4. If project list appears, database connection is working

---

## Support and Diagnostics

### Enable Detailed Logging

Modify `appsettings.json`:

```json
{
  "DatabaseSettings": {
    "ConnectionString": "...",
    "EnableSensitiveDataLogging": true,
    "CommandTimeout": 30
  }
}
```

This will log all SQL queries and connection details to help diagnose issues.

**Warning**: Contains sensitive data - use only for troubleshooting, not in production.

### Common Diagnostic Steps

1. **Verify SQL Server is accessible**
   ```cmd
   sqlcmd -S localhost\SQLEXPRESS -E
   ```

2. **Test connection string**
   Use SQL Server Management Studio with the same parameters

3. **Check Entity Framework migrations**
   ```cmd
   dotnet ef database update --project PIDStandardization.Data --startup-project PIDStandardization.UI
   ```

4. **Review application logs**
   Check Windows Event Viewer → Application logs

---

## Contact and Support

For configuration assistance:
- Review this guide
- Check `appsettings.README.txt` in the Data project
- Consult `USER_GUIDE.md` for general application help
- Consult `DEVELOPER_GUIDE.md` for technical details

---

## Changelog

### v1.1.0 - 2026-01-17
- Added JSON-based configuration system
- Shared configuration between WPF app and AutoCAD plugin
- Support for dynamic connection string configuration
- Backward compatible with default settings

### v1.0.0 - Initial Release
- Hard-coded connection strings
- No external configuration support
