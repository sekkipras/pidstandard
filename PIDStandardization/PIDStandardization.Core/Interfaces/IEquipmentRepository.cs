using PIDStandardization.Core.Entities;

namespace PIDStandardization.Core.Interfaces
{
    /// <summary>
    /// Specialized repository interface for Equipment with optimized queries
    /// </summary>
    public interface IEquipmentRepository : IRepository<Equipment>
    {
        /// <summary>
        /// Get only tag numbers for a project (optimized - no full entity loading)
        /// </summary>
        Task<List<string>> GetTagNumbersAsync(Guid projectId);

        /// <summary>
        /// Check if a tag exists in a project (database-side check)
        /// </summary>
        Task<bool> TagExistsAsync(Guid projectId, string tagNumber);

        /// <summary>
        /// Get maximum sequence number for a tag prefix (optimized query)
        /// </summary>
        Task<int> GetMaxSequenceNumberAsync(Guid projectId, string prefix);

        /// <summary>
        /// Get equipment count by type for a project
        /// </summary>
        Task<Dictionary<string, int>> GetCountByTypeAsync(Guid projectId);

        /// <summary>
        /// Get tag number to equipment ID mapping (optimized projection)
        /// </summary>
        Task<Dictionary<string, Guid>> GetTagToIdMappingAsync(Guid projectId);
    }
}
