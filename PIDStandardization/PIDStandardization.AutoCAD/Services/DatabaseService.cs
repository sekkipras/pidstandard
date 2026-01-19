using Microsoft.EntityFrameworkCore;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Data.Configuration;
using PIDStandardization.Data.Context;
using PIDStandardization.Data.Repositories;
using PIDStandardization.Services;
using Serilog;

namespace PIDStandardization.AutoCAD.Services
{
    /// <summary>
    /// Interface for database service to support dependency injection
    /// </summary>
    public interface IDatabaseService : IDisposable
    {
        IUnitOfWork GetUnitOfWork();
        AuditLogService GetAuditLogService();
    }

    /// <summary>
    /// Service for managing database connections from AutoCAD.
    /// Implements IDatabaseService for testability while maintaining static access for AutoCAD commands.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private IUnitOfWork? _unitOfWork;
        private PIDDbContext? _dbContext;
        private bool _disposed = false;

        // Static instance for backward compatibility with AutoCAD commands
        private static DatabaseService? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance (for AutoCAD command compatibility)
        /// </summary>
        public static DatabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DatabaseService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Constructor for DI scenarios
        /// </summary>
        public DatabaseService()
        {
        }

        /// <summary>
        /// Gets or creates the Unit of Work instance
        /// </summary>
        public IUnitOfWork GetUnitOfWork()
        {
            if (_dbContext == null || _unitOfWork == null)
            {
                try
                {
                    var config = new DatabaseConfiguration();
                    var optionsBuilder = new DbContextOptionsBuilder<PIDDbContext>();
                    optionsBuilder.UseSqlServer(config.ConnectionString)
                                  .UseLazyLoadingProxies();

                    _dbContext = new PIDDbContext(optionsBuilder.Options);
                    _unitOfWork = new UnitOfWork(_dbContext);

                    Log.Debug("Database connection established successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to establish database connection");
                    throw;
                }
            }

            return _unitOfWork;
        }

        /// <summary>
        /// Gets an instance of AuditLogService using the current Unit of Work
        /// </summary>
        public AuditLogService GetAuditLogService()
        {
            return new AuditLogService(GetUnitOfWork());
        }

        /// <summary>
        /// Resets the database connection (useful after errors or for testing)
        /// </summary>
        public void Reset()
        {
            Dispose();
            _disposed = false;
            Log.Debug("Database service reset");
        }

        /// <summary>
        /// Disposes the current database context
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _unitOfWork?.Dispose();
                    _dbContext?.Dispose();
                    _unitOfWork = null;
                    _dbContext = null;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Static method for backward compatibility - gets UnitOfWork from singleton
        /// </summary>
        [Obsolete("Use Instance.GetUnitOfWork() or inject IDatabaseService instead")]
        public static IUnitOfWork GetUnitOfWorkStatic() => Instance.GetUnitOfWork();

        /// <summary>
        /// Static method for backward compatibility - gets AuditLogService from singleton
        /// </summary>
        [Obsolete("Use Instance.GetAuditLogService() or inject IDatabaseService instead")]
        public static AuditLogService GetAuditLogServiceStatic() => Instance.GetAuditLogService();

        /// <summary>
        /// Static dispose for backward compatibility
        /// </summary>
        public static void DisposeStatic()
        {
            lock (_lock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }
    }
}
