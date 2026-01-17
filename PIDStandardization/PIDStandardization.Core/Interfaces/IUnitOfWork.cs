using PIDStandardization.Core.Entities;

namespace PIDStandardization.Core.Interfaces
{
    /// <summary>
    /// Unit of Work pattern for managing transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Project> Projects { get; }
        IRepository<Equipment> Equipment { get; }
        IRepository<Drawing> Drawings { get; }
        IRepository<Line> Lines { get; }
        IRepository<Instrument> Instruments { get; }
        IRepository<ValidationRule> ValidationRules { get; }
        IRepository<BlockMapping> BlockMappings { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
