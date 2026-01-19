# Optimization and Refactoring TODO List

## Overview

This document contains a prioritized action plan for addressing technical debt and optimization opportunities identified in the code review. Tasks are organized by priority with estimated effort and specific implementation steps.

---

## 🔴 CRITICAL PRIORITY (Do This Week)

### 1. Add Structured Logging Infrastructure

**Priority:** Critical
**Effort:** 4-6 hours
**Impact:** High - Essential for production support

**Tasks:**
- [ ] Install Serilog NuGet packages
  - [ ] Add `Serilog` to all projects
  - [ ] Add `Serilog.Sinks.File` for file logging
  - [ ] Add `Serilog.Sinks.Console` for debug output
  - [ ] Add `Serilog.Extensions.Logging` for ILogger integration

- [ ] Configure Serilog in WPF Application
  - [ ] Add configuration in `App.xaml.cs` startup
  - [ ] Set minimum level to Debug
  - [ ] Configure rolling file logs (daily)
  - [ ] Set log file path: `logs/pidstandardization-.txt`

- [ ] Configure Serilog in AutoCAD Plugin
  - [ ] Add initialization in `PluginInitialization.cs`
  - [ ] Configure separate log file for AutoCAD operations

- [ ] Add ILogger to key classes
  - [ ] `PIDCommands.cs` - All AutoCAD commands
  - [ ] `MainWindow.xaml.cs` - UI operations
  - [ ] `AuditLogService.cs` - Service layer
  - [ ] All repository classes

- [ ] Update catch blocks to use logging
  - [ ] Replace `ed.WriteMessage($"Error: {ex.Message}")`
  - [ ] Add `_logger.LogError(ex, "Operation failed...")`
  - [ ] Keep user-friendly messages for UI
  - [ ] Log full exception with stack trace

**Files to Modify:**
- `App.xaml.cs`
- `PluginInitialization.cs`
- `PIDCommands.cs` (30+ catch blocks)
- `MainWindow.xaml.cs` (20+ catch blocks)
- All service classes

**Testing:**
- [ ] Verify log files created in logs folder
- [ ] Trigger errors and verify stack traces logged
- [ ] Check log file rotation works
- [ ] Verify no performance impact

---

### 2. Fix N+1 Query Performance Issue

**Priority:** Critical
**Effort:** 2-3 hours
**Impact:** High - 10-15x performance improvement

**Tasks:**
- [ ] Identify all locations loading full equipment entities
  - [ ] `MainWindow.xaml.cs` Line 367-368
  - [ ] Import services (Equipment, Lines, Instruments)
  - [ ] Tag validation service

- [ ] Refactor equipment loading with projection
  - [ ] Change from `FindAsync(e => e.ProjectId == id)`
  - [ ] To `FindAsync(e => e.ProjectId == id).Select(e => e.TagNumber)`
  - [ ] Return `IQueryable<string>` for tag numbers only

- [ ] Update duplicate check logic
  ```csharp
  // Before
  var existingEquipment = await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == projectId);
  var existingTags = existingEquipment.Select(e => e.TagNumber).ToList();

  // After
  var existingTags = await _unitOfWork.Equipment
      .Where(e => e.ProjectId == projectId)
      .Select(e => e.TagNumber)
      .ToListAsync();
  ```

- [ ] Add database-side duplicate checking
  ```csharp
  public async Task<bool> TagExistsAsync(Guid projectId, string tagNumber)
  {
      return await _dbSet.AnyAsync(e =>
          e.ProjectId == projectId &&
          e.TagNumber == tagNumber);
  }
  ```

- [ ] Add method to IEquipmentRepository
  - [ ] `Task<List<string>> GetTagNumbersAsync(Guid projectId)`
  - [ ] `Task<bool> TagExistsAsync(Guid projectId, string tagNumber)`

**Files to Modify:**
- `IEquipmentRepository.cs` (add new methods)
- `Repository.cs` (implement new methods)
- `MainWindow.xaml.cs` (Lines 367-368, 692-693)
- `ExcelImportService.cs` (Lines 60-75, similar patterns)

**Testing:**
- [ ] Test with small dataset (10 items)
- [ ] Test with large dataset (1,000+ items)
- [ ] Measure performance improvement
- [ ] Verify no data loss or corruption

**Expected Results:**
- Before: 2-3 seconds for 10,000 items
- After: 0.2 seconds for 10,000 items

---

### 3. Optimize Tag Generation Query

**Priority:** Critical
**Effort:** 3-4 hours
**Impact:** High - 20-40x performance improvement

**Tasks:**
- [ ] Add efficient max sequence number query
  ```csharp
  public interface IEquipmentRepository : IRepository<Equipment>
  {
      Task<int> GetMaxSequenceNumberAsync(Guid projectId, string prefix);
  }
  ```

- [ ] Implement using SQL query
  ```csharp
  public async Task<int> GetMaxSequenceNumberAsync(Guid projectId, string prefix)
  {
      var result = await _dbSet
          .Where(e => e.ProjectId == projectId &&
                      e.TagNumber.StartsWith(prefix))
          .Select(e => e.TagNumber)
          .ToListAsync();

      var maxNumber = result
          .Select(tag => ExtractSequenceNumber(tag, prefix))
          .Where(num => num.HasValue)
          .DefaultIfEmpty(0)
          .Max();

      return maxNumber ?? 0;
  }

  private int? ExtractSequenceNumber(string tagNumber, string prefix)
  {
      var numberPart = tagNumber.Substring(prefix.Length).TrimStart('-');
      return int.TryParse(numberPart, out int num) ? num : null;
  }
  ```

- [ ] Update TagValidationService.cs
  - [ ] Replace Lines 81-89
  - [ ] Use new `GetMaxSequenceNumberAsync` method
  - [ ] Remove loading of all equipment

- [ ] Update PIDCommands.cs tag generation
  - [ ] Lines 83-97 (TagEquipment)
  - [ ] Lines 271-284 (BatchTagEquipment)
  - [ ] Use repository method instead of local logic

**Files to Modify:**
- `IEquipmentRepository.cs`
- `Repository.cs` (add new method)
- `TagValidationService.cs` (Lines 81-89)
- `PIDCommands.cs` (Lines 83-97, 271-284)

**Testing:**
- [ ] Test with various tag formats (P-001, TK-0001, etc.)
- [ ] Test with mixed prefixes in same project
- [ ] Verify correct sequence numbers generated
- [ ] Performance test with 10,000+ items

**Expected Results:**
- Before: 1-2 seconds
- After: 0.05 seconds

---

### 4. Improve Error Messages for Users

**Priority:** Critical
**Effort:** 6-8 hours
**Impact:** High - Better user experience

**Tasks:**
- [ ] Create error message helper class
  ```csharp
  public static class UserErrorMessages
  {
      public static string GetDatabaseConnectionError(SqlException ex)
      {
          return ex.Number switch
          {
              -2 => "Database connection timed out.\n\n" +
                    "Please check:\n" +
                    "• SQL Server is running\n" +
                    "• Network connection is available\n" +
                    "• Firewall allows database access",
              4060 => "Database not found.\n\n" +
                      "Please run database migration first.\n" +
                      "See MIGRATION_README.md for instructions.",
              18456 => "Login failed - invalid credentials.\n\n" +
                       "Please check your database connection settings.",
              _ => $"Database error occurred.\n\n" +
                   $"Error code: {ex.Number}\n" +
                   $"Contact your administrator for assistance."
          };
      }

      public static string GetFileAccessError(IOException ex)
      {
          if (ex.Message.Contains("being used by another process"))
          {
              return "File is currently open in another application.\n\n" +
                     "Please close the file and try again.";
          }
          if (ex.Message.Contains("Access denied"))
          {
              return "Access denied to file.\n\n" +
                     "Please check file permissions.";
          }
          return "File access error occurred.\n\n" +
                 "Please check file path and permissions.";
      }

      public static string GetDuplicateTagError(string tagNumber)
      {
          return $"Tag number already exists: {tagNumber}\n\n" +
                 $"This tag is already used in this project.\n\n" +
                 $"Would you like to:\n" +
                 $"• Use a different tag number\n" +
                 $"• View existing equipment\n" +
                 $"• Auto-generate next available tag";
      }
  }
  ```

- [ ] Update all catch blocks in UI layer
  - [ ] Replace generic `MessageBox.Show(ex.Message)`
  - [ ] Use helper class for common errors
  - [ ] Add specific handling for SqlException, IOException
  - [ ] Keep technical details in logs only

- [ ] Update AutoCAD command error messages
  - [ ] Replace `ed.WriteMessage($"\nError: {ex.Message}")`
  - [ ] Provide actionable guidance
  - [ ] Differentiate between user errors and system errors

- [ ] Add error dialog with details expansion
  ```csharp
  public class ErrorDetailsDialog : Window
  {
      public string UserMessage { get; set; }
      public string TechnicalDetails { get; set; }
      public Guid CorrelationId { get; set; }
  }
  ```

**Files to Modify:**
- Create: `Helpers/UserErrorMessages.cs`
- Create: `Views/ErrorDetailsDialog.xaml`
- `MainWindow.xaml.cs` (all catch blocks)
- `PIDCommands.cs` (all catch blocks)
- All dialog classes with error handling

**Testing:**
- [ ] Test database connection timeout
- [ ] Test file locked error
- [ ] Test duplicate tag error
- [ ] Test invalid data error
- [ ] Verify technical details in log file

---

## 🟠 HIGH PRIORITY (Do This Month)

### 5. Refactor DatabaseService from Static Singleton

**Priority:** High
**Effort:** 8-12 hours
**Impact:** Medium - Enables testing and proper DI

**Tasks:**
- [ ] Create IDatabaseService interface
  ```csharp
  public interface IDatabaseService
  {
      IUnitOfWork GetUnitOfWork();
      void Dispose();
  }
  ```

- [ ] Refactor DatabaseService to instance-based
  - [ ] Remove static fields
  - [ ] Add constructor with IDbContextFactory
  - [ ] Implement IDisposable
  - [ ] Use scoped lifetime

- [ ] Add dependency injection container to AutoCAD plugin
  - [ ] Install Microsoft.Extensions.DependencyInjection
  - [ ] Configure services in PluginInitialization
  - [ ] Register DbContextFactory, repositories, services

- [ ] Update all AutoCAD commands
  - [ ] Remove static calls to DatabaseService.GetUnitOfWork()
  - [ ] Inject IDatabaseService via constructor or property
  - [ ] Use service locator pattern if DI not possible

- [ ] Update WPF application DI
  - [ ] Already using DI - ensure consistency
  - [ ] Register IDatabaseService as scoped
  - [ ] Update service registrations

**Files to Create:**
- `IDatabaseService.cs`

**Files to Modify:**
- `DatabaseService.cs` (major refactor)
- `PluginInitialization.cs` (add DI setup)
- `PIDCommands.cs` (all command methods)
- `App.xaml.cs` (update DI registration)

**Testing:**
- [ ] Verify all commands work with new pattern
- [ ] Test multiple command invocations
- [ ] Check memory usage (no leaks)
- [ ] Verify DbContext properly disposed

---

### 6. Extract Configuration from Hardcoded Values

**Priority:** High
**Effort:** 6-8 hours
**Impact:** Medium - Improved flexibility

**Tasks:**
- [ ] Create configuration models
  ```csharp
  public class EquipmentTypeConfiguration
  {
      public List<EquipmentTypeMapping> BlockNameMappings { get; set; }
  }

  public class EquipmentTypeMapping
  {
      public string Pattern { get; set; }  // Regex pattern
      public string Type { get; set; }     // Equipment type name
      public string Prefix { get; set; }   // Tag prefix
  }

  public class TagFormatConfiguration
  {
      public string ISA { get; set; }  // {PREFIX}-{SEQ:000}
      public string KKS { get; set; }  // ={AREA}{TYPE}-{SEQ:0000}
  }

  public class StorageConfiguration
  {
      public string DrawingsPath { get; set; }
      public string BackupsPath { get; set; }
      public int MaxFileSizeMB { get; set; }
  }
  ```

- [ ] Update appsettings.json
  ```json
  {
    "EquipmentTypes": {
      "BlockNameMappings": [
        { "Pattern": "PUMP|PMP", "Type": "Pump", "Prefix": "P" },
        { "Pattern": "VALVE|VLV", "Type": "Valve", "Prefix": "VLV" },
        { "Pattern": "TANK|TK|TNK", "Type": "Tank", "Prefix": "TK" }
      ]
    },
    "TagFormats": {
      "ISA": "{PREFIX}-{SEQ:000}",
      "KKS": "={AREA}{TYPE}-{SEQ:0000}"
    },
    "Storage": {
      "DrawingsPath": "C:\\ProgramData\\PIDStandardization\\Drawings",
      "BackupsPath": "C:\\ProgramData\\PIDStandardization\\Backups",
      "MaxFileSizeMB": 100
    }
  }
  ```

- [ ] Create configuration service
  - [ ] Load and validate configuration on startup
  - [ ] Provide strongly-typed access
  - [ ] Support hot-reload (optional)

- [ ] Replace hardcoded values
  - [ ] PIDCommands.cs equipment type detection
  - [ ] EquipmentExtractionService.cs prefixes
  - [ ] MainWindow.xaml.cs file paths
  - [ ] Tag format strings

**Files to Create:**
- `Configuration/EquipmentTypeConfiguration.cs`
- `Configuration/TagFormatConfiguration.cs`
- `Configuration/StorageConfiguration.cs`
- `Services/ConfigurationService.cs`

**Files to Modify:**
- `appsettings.json` (both UI and AutoCAD)
- `PIDCommands.cs` (Lines 428-450, 81, 96)
- `EquipmentExtractionService.cs` (Lines 15-26)
- `MainWindow.xaml.cs` (Lines 1133-1134)

**Testing:**
- [ ] Test with default configuration
- [ ] Test with custom equipment types
- [ ] Test with custom tag formats
- [ ] Verify validation catches invalid config

---

### 7. Fix async void Methods

**Priority:** High
**Effort:** 2-3 hours
**Impact:** Medium - Proper exception handling

**Tasks:**
- [ ] Identify all async void methods
  - [ ] `PIDCommands.cs` Line 20: `TagEquipment()`
  - [ ] `PIDCommands.cs` Line 212: `BatchTagEquipment()`
  - [ ] `PIDCommands.cs` Line 491: `ExtractEquipment()`

- [ ] Refactor to async Task with synchronous wrapper
  ```csharp
  // Before
  [CommandMethod("PIDTAG")]
  public async void TagEquipment()
  {
      // ... implementation
  }

  // After
  [CommandMethod("PIDTAG")]
  public void TagEquipment()
  {
      try
      {
          TagEquipmentAsync().GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, "Error in PIDTAG command");
          var doc = Application.DocumentManager.MdiActiveDocument;
          doc.Editor.WriteMessage($"\nCommand failed: {ex.Message}");
      }
  }

  private async Task TagEquipmentAsync()
  {
      // ... existing implementation moved here
  }
  ```

- [ ] Add proper exception handling to wrappers
  - [ ] Log exceptions
  - [ ] Display user-friendly messages
  - [ ] Ensure AutoCAD doesn't crash

**Files to Modify:**
- `PIDCommands.cs` (Lines 20-210, 212-489, 491-785)

**Testing:**
- [ ] Test all three commands
- [ ] Trigger exceptions and verify handling
- [ ] Verify AutoCAD stability
- [ ] Check error messages displayed

---

### 8. Extract Duplicate Code to Shared Methods

**Priority:** High
**Effort:** 4-6 hours
**Impact:** Medium - Code maintainability

**Tasks:**
- [ ] Create TagGenerationService
  ```csharp
  public interface ITagGenerationService
  {
      Task<string> GenerateNextTagAsync(Guid projectId, string prefix);
      Task<Dictionary<string, int>> GetTagCountersAsync(Guid projectId);
  }

  public class TagGenerationService : ITagGenerationService
  {
      private readonly IUnitOfWork _unitOfWork;

      public async Task<string> GenerateNextTagAsync(Guid projectId, string prefix)
      {
          var maxNumber = await _unitOfWork.Equipment
              .GetMaxSequenceNumberAsync(projectId, prefix);
          return $"{prefix}-{(maxNumber + 1):D3}";
      }

      public async Task<Dictionary<string, int>> GetTagCountersAsync(Guid projectId)
      {
          // Shared implementation
      }
  }
  ```

- [ ] Extract tag parsing logic
  - [ ] Create `ParseTagNumber(string tag)` method
  - [ ] Create `ExtractPrefix(string tag)` method
  - [ ] Create `ExtractSequence(string tag)` method

- [ ] Update PIDCommands to use shared service
  - [ ] Remove duplicate code from TagEquipment (Lines 83-97)
  - [ ] Remove duplicate code from BatchTagEquipment (Lines 271-284)
  - [ ] Call shared service instead

- [ ] Create EquipmentTypeDetectorService
  ```csharp
  public interface IEquipmentTypeDetector
  {
      string? DetectType(string blockName);
      string? GetPrefix(string equipmentType);
  }
  ```

- [ ] Update both locations to use shared detector
  - [ ] PIDCommands.cs GetEquipmentTypeFromBlockName
  - [ ] EquipmentExtractionService.cs detection logic

**Files to Create:**
- `Services/TagGenerationService.cs`
- `Services/EquipmentTypeDetectorService.cs`

**Files to Modify:**
- `PIDCommands.cs` (Lines 83-97, 271-284, 428-450)
- `EquipmentExtractionService.cs` (Lines 15-26, 99-114)

**Testing:**
- [ ] Test tag generation with various formats
- [ ] Test equipment type detection
- [ ] Verify backward compatibility
- [ ] Test edge cases (empty tags, special chars)

---

## 🟡 MEDIUM PRIORITY (Do Next Quarter)

### 9. Add Exception Handling to Repository Layer

**Priority:** Medium
**Effort:** 3-4 hours
**Impact:** Low - Better error isolation

**Tasks:**
- [ ] Create custom exception types
  ```csharp
  public class DataAccessException : Exception
  {
      public DataAccessException(string message, Exception innerException)
          : base(message, innerException) { }
  }

  public class EntityNotFoundException : DataAccessException
  {
      public EntityNotFoundException(string entityType, Guid id)
          : base($"{entityType} with ID {id} not found", null) { }
  }
  ```

- [ ] Add try-catch to all repository methods
  - [ ] GetByIdAsync
  - [ ] GetAllAsync
  - [ ] FindAsync
  - [ ] AddAsync
  - [ ] UpdateAsync
  - [ ] DeleteAsync

- [ ] Log exceptions at repository level
  - [ ] Add ILogger to Repository<T>
  - [ ] Log with entity type and operation details

**Files to Create:**
- `Exceptions/DataAccessException.cs`
- `Exceptions/EntityNotFoundException.cs`

**Files to Modify:**
- `Repository.cs` (all methods Lines 22-63)

**Testing:**
- [ ] Test database connection failure
- [ ] Test entity not found
- [ ] Test duplicate key violation
- [ ] Verify exceptions properly wrapped

---

### 10. Fix Silent Configuration Load Failures

**Priority:** Medium
**Effort:** 2-3 hours
**Impact:** Low - Better diagnostics

**Tasks:**
- [ ] Update DatabaseConfiguration.cs
  - [ ] Remove empty catch block (Line 77)
  - [ ] Add proper error handling
  - [ ] Log configuration errors
  - [ ] Provide fallback indication

- [ ] Add configuration validation
  - [ ] Check connection string format
  - [ ] Test database connectivity on startup
  - [ ] Show warning if using default config

**Files to Modify:**
- `DatabaseConfiguration.cs` (Lines 54-81)

**Testing:**
- [ ] Test with missing config file
- [ ] Test with invalid JSON
- [ ] Test with invalid connection string
- [ ] Verify logging works

---

### 11. Replace Fire-and-Forget Async

**Priority:** Medium
**Effort:** 2-3 hours
**Impact:** Low - Prevent race conditions

**Tasks:**
- [ ] Find all fire-and-forget patterns
  - [ ] Search for `_ = SomeMethodAsync()`
  - [ ] MainWindow.xaml.cs Line 1483

- [ ] Refactor to await or safe background execution
  ```csharp
  // Before
  _ = LoadEquipmentForProject(project.ProjectId);

  // After Option 1: Await it
  await LoadEquipmentForProject(project.ProjectId);

  // After Option 2: Safe fire-and-forget
  _ = LoadEquipmentForProjectSafeAsync(project.ProjectId);

  private async Task LoadEquipmentForProjectSafeAsync(Guid projectId)
  {
      try
      {
          await LoadEquipmentForProject(projectId);
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, "Background load failed");
      }
  }
  ```

**Files to Modify:**
- `MainWindow.xaml.cs` (Line 1483, check for others)

**Testing:**
- [ ] Test concurrent operations
- [ ] Verify no race conditions
- [ ] Check exception handling

---

### 12. Implement Caching Strategy

**Priority:** Medium
**Effort:** 6-8 hours
**Impact:** Medium - Performance improvement

**Tasks:**
- [ ] Add IMemoryCache to WPF application
  - [ ] Install Microsoft.Extensions.Caching.Memory
  - [ ] Register in DI container
  - [ ] Configure cache settings

- [ ] Create cached service wrappers
  ```csharp
  public class CachedProjectService : IProjectService
  {
      private readonly IProjectService _inner;
      private readonly IMemoryCache _cache;
      private readonly ILogger<CachedProjectService> _logger;

      public async Task<IEnumerable<Project>> GetAllAsync()
      {
          return await _cache.GetOrCreateAsync("all-projects", async entry =>
          {
              entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
              _logger.LogDebug("Loading projects from database");
              return await _inner.GetAllAsync();
          });
      }

      public async Task UpdateAsync(Project project)
      {
          await _inner.UpdateAsync(project);
          _cache.Remove("all-projects");  // Invalidate
          _logger.LogDebug("Projects cache invalidated");
      }
  }
  ```

- [ ] Cache frequently accessed data
  - [ ] Project list (5 minute sliding)
  - [ ] Equipment types configuration (1 hour absolute)
  - [ ] Current project equipment (2 minute sliding)

- [ ] Add cache invalidation
  - [ ] On data modification (add/update/delete)
  - [ ] Manual refresh button
  - [ ] Time-based expiration

**Files to Create:**
- `Services/Cached/CachedProjectService.cs`
- `Services/Cached/CachedEquipmentService.cs`

**Files to Modify:**
- `App.xaml.cs` (DI registration)
- Service interfaces (add cache decorators)

**Testing:**
- [ ] Test cache hit/miss
- [ ] Test cache invalidation
- [ ] Measure performance improvement
- [ ] Test with stale data scenarios

---

### 13. Add Unit Tests

**Priority:** Medium
**Effort:** 12-16 hours
**Impact:** High - Code quality and confidence

**Tasks:**
- [ ] Create test projects
  - [ ] PIDStandardization.Core.Tests
  - [ ] PIDStandardization.Services.Tests
  - [ ] PIDStandardization.Data.Tests

- [ ] Install testing frameworks
  - [ ] xUnit or NUnit
  - [ ] Moq for mocking
  - [ ] FluentAssertions for readable assertions
  - [ ] Microsoft.EntityFrameworkCore.InMemory

- [ ] Write tests for core logic
  - [ ] Tag validation tests
  - [ ] Equipment type detection tests
  - [ ] Tag generation tests
  - [ ] Sequence number parsing tests

- [ ] Write tests for services
  - [ ] AuditLogService tests
  - [ ] TagGenerationService tests
  - [ ] EquipmentTypeDetector tests

- [ ] Write tests for repositories
  - [ ] Use InMemory database
  - [ ] Test CRUD operations
  - [ ] Test query methods

- [ ] Set up CI/CD pipeline (optional)
  - [ ] Run tests on every commit
  - [ ] Generate code coverage report
  - [ ] Fail build if tests fail

**Files to Create:**
- Entire test project structure
- 50+ test classes

**Testing:**
- [ ] Achieve 60%+ code coverage
- [ ] All tests pass
- [ ] Tests run in <30 seconds

---

## 🟢 LOW PRIORITY (Future Refactoring)

### 14. Refactor UI to MVVM Pattern

**Priority:** Low
**Effort:** 20-30 hours
**Impact:** Medium - Long-term maintainability

**Tasks:**
- [ ] Install MVVM framework (CommunityToolkit.Mvvm)
- [ ] Create ViewModels for each window/dialog
- [ ] Extract business logic from code-behind
- [ ] Implement ICommand for button actions
- [ ] Use data binding instead of manual UI updates
- [ ] Create services for data access
- [ ] Unit test ViewModels

**Status:** Defer to future sprint - large refactoring effort

---

### 15. Optimize Excel Import/Export

**Priority:** Low
**Effort:** 8-12 hours
**Impact:** Medium - Better performance for large files

**Tasks:**
- [ ] Implement streaming for large files
- [ ] Add progress reporting during import/export
- [ ] Use async/await throughout
- [ ] Add cancellation token support
- [ ] Optimize memory usage

**Status:** Works acceptably for current use cases - optimize if needed

---

## 📊 Progress Tracking

### Overall Progress

| Priority Level | Total Tasks | Completed | In Progress | Pending |
|----------------|-------------|-----------|-------------|---------|
| Critical       | 4           | 0         | 0           | 4       |
| High           | 4           | 0         | 0           | 4       |
| Medium         | 5           | 0         | 0           | 5       |
| Low            | 2           | 0         | 0           | 2       |
| **TOTAL**      | **15**      | **0**     | **0**       | **15**  |

### Critical Tasks Status

- [ ] Add Serilog logging infrastructure (0%)
- [ ] Fix N+1 query performance (0%)
- [ ] Optimize tag generation query (0%)
- [ ] Improve error messages (0%)

### Estimated Timeline

**Week 1:** Critical tasks (4 tasks, ~20 hours)
- [ ] Monday-Tuesday: Logging infrastructure
- [ ] Wednesday: N+1 query fixes
- [ ] Thursday: Tag generation optimization
- [ ] Friday: Error message improvements

**Week 2-4:** High priority tasks (4 tasks, ~30 hours)
- [ ] Week 2: DatabaseService refactoring
- [ ] Week 3: Configuration extraction
- [ ] Week 3: Fix async void methods
- [ ] Week 4: Extract duplicate code

**Month 2-3:** Medium priority tasks (5 tasks, ~30 hours)
- [ ] Repository exception handling
- [ ] Configuration fixes
- [ ] Fire-and-forget fixes
- [ ] Caching implementation
- [ ] Unit tests

**Future:** Low priority (defer)
- [ ] MVVM refactoring
- [ ] Excel optimization

---

## 🎯 Success Criteria

### Performance Targets

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Load 10K equipment | 2-3 sec | < 0.5 sec | ⏳ Pending |
| Tag generation | 1-2 sec | < 0.1 sec | ⏳ Pending |
| Import 1K rows | 30 sec | < 10 sec | ⏳ Pending |

### Code Quality Targets

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Generic catch blocks | 30+ | 0 | ⏳ Pending |
| Hardcoded values | 50+ | < 10 | ⏳ Pending |
| Code duplication | 15% | < 5% | ⏳ Pending |
| Unit test coverage | 0% | > 60% | ⏳ Pending |
| Async void methods | 3 | 0 | ⏳ Pending |

### Production Readiness

- [ ] Structured logging implemented
- [ ] All critical performance issues fixed
- [ ] User-friendly error messages
- [ ] Configuration externalized
- [ ] Zero async void methods
- [ ] Core business logic tested
- [ ] Documentation updated

---

## 📝 Notes

### Dependencies Between Tasks

- **Logging must be done first** - All other tasks benefit from logging
- **DatabaseService refactoring** - Enables proper DI for other tasks
- **Configuration extraction** - Should be done before unit tests
- **Unit tests** - Should be added as other tasks are completed

### Risk Mitigation

- Test all changes in development environment first
- Maintain backward compatibility
- Keep old code in branches during refactoring
- Update documentation as code changes
- Get user feedback on error message improvements

### Resources Needed

- Development environment with .NET 8 SDK
- SQL Server instance for testing
- AutoCAD 2026 for plugin testing
- Time allocation: 1-2 developers for 2-3 months

---

**Document Version:** 1.0
**Created:** 2026-01-18
**Last Updated:** 2026-01-18
**Next Review:** After completing critical tasks

---

## Quick Start

**To begin optimization work:**

1. Start with Critical Priority items in order (1-4)
2. Create a branch: `feature/optimization-critical`
3. Complete one task at a time
4. Test thoroughly before moving to next task
5. Commit and document each completed task
6. Update this TODO list as you progress

**Happy optimizing! 🚀**
