using Microsoft.EntityFrameworkCore;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Data.Context;
using Serilog;
using System.Linq.Expressions;

namespace PIDStandardization.Data.Repositories
{
    /// <summary>
    /// Generic repository implementation with comprehensive error handling
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly PIDDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly string _entityTypeName;

        public Repository(PIDDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _entityTypeName = typeof(T).Name;
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            try
            {
                Log.Debug("Getting {EntityType} by ID: {Id}", _entityTypeName, id);
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting {EntityType} by ID: {Id}", _entityTypeName, id);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                Log.Debug("Getting all {EntityType} entities", _entityTypeName);
                var result = await _dbSet.ToListAsync();
                Log.Debug("Retrieved {Count} {EntityType} entities", result.Count, _entityTypeName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all {EntityType} entities", _entityTypeName);
                throw;
            }
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                Log.Debug("Finding {EntityType} entities with predicate", _entityTypeName);
                var result = await _dbSet.Where(predicate).ToListAsync();
                Log.Debug("Found {Count} {EntityType} entities matching predicate", result.Count, _entityTypeName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error finding {EntityType} entities with predicate", _entityTypeName);
                throw;
            }
        }

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                Log.Debug("Adding new {EntityType} entity", _entityTypeName);
                await _dbSet.AddAsync(entity);
                Log.Debug("Added new {EntityType} entity successfully", _entityTypeName);
                return entity;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding {EntityType} entity", _entityTypeName);
                throw;
            }
        }

        public Task UpdateAsync(T entity)
        {
            try
            {
                Log.Debug("Updating {EntityType} entity", _entityTypeName);
                _dbSet.Update(entity);
                Log.Debug("Updated {EntityType} entity successfully", _entityTypeName);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating {EntityType} entity", _entityTypeName);
                throw;
            }
        }

        public Task DeleteAsync(T entity)
        {
            try
            {
                Log.Debug("Deleting {EntityType} entity", _entityTypeName);
                _dbSet.Remove(entity);
                Log.Debug("Deleted {EntityType} entity successfully", _entityTypeName);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting {EntityType} entity", _entityTypeName);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                Log.Debug("Checking existence of {EntityType} entity", _entityTypeName);
                return await _dbSet.AnyAsync(predicate);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking existence of {EntityType} entity", _entityTypeName);
                throw;
            }
        }

        public async Task<int> CountAsync()
        {
            try
            {
                Log.Debug("Counting {EntityType} entities", _entityTypeName);
                var count = await _dbSet.CountAsync();
                Log.Debug("Count of {EntityType} entities: {Count}", _entityTypeName, count);
                return count;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error counting {EntityType} entities", _entityTypeName);
                throw;
            }
        }
    }
}
