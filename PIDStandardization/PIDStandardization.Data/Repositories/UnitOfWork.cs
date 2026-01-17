using Microsoft.EntityFrameworkCore.Storage;
using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Data.Context;

namespace PIDStandardization.Data.Repositories
{
    /// <summary>
    /// Unit of Work implementation for managing database transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PIDDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(PIDDbContext context)
        {
            _context = context;

            // Initialize repositories
            Projects = new Repository<Project>(_context);
            Equipment = new Repository<Equipment>(_context);
            Drawings = new Repository<Drawing>(_context);
            Lines = new Repository<Line>(_context);
            Instruments = new Repository<Instrument>(_context);
            ValidationRules = new Repository<ValidationRule>(_context);
            BlockMappings = new Repository<BlockMapping>(_context);
        }

        public IRepository<Project> Projects { get; private set; }
        public IRepository<Equipment> Equipment { get; private set; }
        public IRepository<Drawing> Drawings { get; private set; }
        public IRepository<Line> Lines { get; private set; }
        public IRepository<Instrument> Instruments { get; private set; }
        public IRepository<ValidationRule> ValidationRules { get; private set; }
        public IRepository<BlockMapping> BlockMappings { get; private set; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
