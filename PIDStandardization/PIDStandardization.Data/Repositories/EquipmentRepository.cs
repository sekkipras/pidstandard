using Microsoft.EntityFrameworkCore;
using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Data.Context;
using Serilog;
using System.Text.RegularExpressions;

namespace PIDStandardization.Data.Repositories
{
    /// <summary>
    /// Optimized Equipment repository with efficient queries
    /// </summary>
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        public EquipmentRepository(PIDDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get only tag numbers (projection - much faster than loading full entities)
        /// </summary>
        public async Task<List<string>> GetTagNumbersAsync(Guid projectId)
        {
            try
            {
                return await _dbSet
                    .Where(e => e.ProjectId == projectId && e.IsActive)
                    .Select(e => e.TagNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting tag numbers for project {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// Database-side check for tag existence (very fast)
        /// </summary>
        public async Task<bool> TagExistsAsync(Guid projectId, string tagNumber)
        {
            try
            {
                return await _dbSet.AnyAsync(e =>
                    e.ProjectId == projectId &&
                    e.TagNumber == tagNumber &&
                    e.IsActive);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking tag existence: {TagNumber}, ProjectId: {ProjectId}",
                    tagNumber, projectId);
                throw;
            }
        }

        /// <summary>
        /// Get max sequence number for a prefix (optimized - loads only relevant data)
        /// </summary>
        public async Task<int> GetMaxSequenceNumberAsync(Guid projectId, string prefix)
        {
            try
            {
                // Get all tag numbers with the prefix
                var tags = await _dbSet
                    .Where(e => e.ProjectId == projectId &&
                                e.TagNumber.StartsWith(prefix) &&
                                e.IsActive)
                    .Select(e => e.TagNumber)
                    .ToListAsync();

                if (!tags.Any())
                {
                    return 0;
                }

                // Extract sequence numbers in memory (parsing logic)
                var maxNumber = tags
                    .Select(tag => ExtractSequenceNumber(tag, prefix))
                    .Where(num => num.HasValue)
                    .DefaultIfEmpty(0)
                    .Max();

                return maxNumber ?? 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting max sequence number for prefix {Prefix}, ProjectId: {ProjectId}",
                    prefix, projectId);
                throw;
            }
        }

        /// <summary>
        /// Extract sequence number from tag
        /// </summary>
        private int? ExtractSequenceNumber(string tagNumber, string prefix)
        {
            try
            {
                // Remove prefix and any separators
                var numberPart = tagNumber.Substring(prefix.Length).TrimStart('-', '_');

                // Extract numeric part using regex
                var match = Regex.Match(numberPart, @"^\d+");
                if (match.Success && int.TryParse(match.Value, out int num))
                {
                    return num;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get equipment count grouped by type (aggregation at database level)
        /// </summary>
        public async Task<Dictionary<string, int>> GetCountByTypeAsync(Guid projectId)
        {
            try
            {
                return await _dbSet
                    .Where(e => e.ProjectId == projectId && e.IsActive)
                    .GroupBy(e => e.EquipmentType ?? "Unknown")
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting equipment count by type for ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// Get tag to ID mapping (only loads TagNumber and EquipmentId - optimized)
        /// </summary>
        public async Task<Dictionary<string, Guid>> GetTagToIdMappingAsync(Guid projectId)
        {
            try
            {
                return await _dbSet
                    .Where(e => e.ProjectId == projectId && e.IsActive)
                    .Select(e => new { e.TagNumber, e.EquipmentId })
                    .ToDictionaryAsync(x => x.TagNumber, x => x.EquipmentId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting tag to ID mapping for ProjectId: {ProjectId}", projectId);
                throw;
            }
        }
    }
}
