using Microsoft.EntityFrameworkCore;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Data.Configuration;
using PIDStandardization.Data.Context;
using PIDStandardization.Data.Repositories;

namespace PIDStandardization.AutoCAD.Services
{
    /// <summary>
    /// Service for managing database connections from AutoCAD
    /// </summary>
    public class DatabaseService
    {
        private static IUnitOfWork? _unitOfWork;
        private static PIDDbContext? _dbContext;

        /// <summary>
        /// Gets or creates the Unit of Work instance
        /// </summary>
        public static IUnitOfWork GetUnitOfWork()
        {
            if (_dbContext == null || _unitOfWork == null)
            {
                var config = new DatabaseConfiguration();
                var optionsBuilder = new DbContextOptionsBuilder<PIDDbContext>();
                optionsBuilder.UseSqlServer(config.ConnectionString)
                              .UseLazyLoadingProxies(); // Enable lazy loading for navigation properties

                _dbContext = new PIDDbContext(optionsBuilder.Options);
                _unitOfWork = new UnitOfWork(_dbContext);
            }

            return _unitOfWork;
        }

        /// <summary>
        /// Disposes the current database context
        /// </summary>
        public static void Dispose()
        {
            _unitOfWork?.Dispose();
            _dbContext?.Dispose();
            _unitOfWork = null;
            _dbContext = null;
        }
    }
}
