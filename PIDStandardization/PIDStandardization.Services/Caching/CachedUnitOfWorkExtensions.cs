using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using Serilog;

namespace PIDStandardization.Services.Caching
{
    /// <summary>
    /// Extension methods for IUnitOfWork to provide cached data access
    /// </summary>
    public static class CachedUnitOfWorkExtensions
    {
        private static readonly ICacheService _cache = new MemoryCacheService(TimeSpan.FromMinutes(5));

        /// <summary>
        /// Gets all projects with caching
        /// </summary>
        public static async Task<IEnumerable<Project>> GetAllProjectsCachedAsync(this IUnitOfWork unitOfWork)
        {
            var cacheKey = MemoryCacheService.CacheKeys.Projects;

            if (_cache.TryGetValue<IEnumerable<Project>>(cacheKey, out var cachedProjects) && cachedProjects != null)
            {
                return cachedProjects;
            }

            var projects = await unitOfWork.Projects.GetAllAsync();
            _cache.Set(cacheKey, projects, TimeSpan.FromMinutes(10));
            return projects;
        }

        /// <summary>
        /// Gets equipment tag numbers for a project with caching
        /// </summary>
        public static async Task<List<string>> GetEquipmentTagsCachedAsync(this IUnitOfWork unitOfWork, Guid projectId)
        {
            var cacheKey = MemoryCacheService.CacheKeys.EquipmentTagsByProject(projectId);

            if (_cache.TryGetValue<List<string>>(cacheKey, out var cachedTags) && cachedTags != null)
            {
                return cachedTags;
            }

            var tags = await unitOfWork.Equipment.GetTagNumbersAsync(projectId);
            _cache.Set(cacheKey, tags, TimeSpan.FromMinutes(5));
            return tags;
        }

        /// <summary>
        /// Gets equipment by project with caching
        /// </summary>
        public static async Task<IEnumerable<Equipment>> GetEquipmentByProjectCachedAsync(
            this IUnitOfWork unitOfWork, Guid projectId)
        {
            var cacheKey = MemoryCacheService.CacheKeys.EquipmentByProject(projectId);

            if (_cache.TryGetValue<IEnumerable<Equipment>>(cacheKey, out var cachedEquipment) && cachedEquipment != null)
            {
                return cachedEquipment;
            }

            var equipment = await unitOfWork.Equipment.FindAsync(e => e.ProjectId == projectId && e.IsActive);
            _cache.Set(cacheKey, equipment, TimeSpan.FromMinutes(5));
            return equipment;
        }

        /// <summary>
        /// Invalidates project cache when projects are modified
        /// </summary>
        public static void InvalidateProjectCache(this IUnitOfWork unitOfWork)
        {
            _cache.RemoveByPrefix(MemoryCacheService.CacheKeys.Projects);
            Log.Debug("Project cache invalidated");
        }

        /// <summary>
        /// Invalidates equipment cache for a specific project
        /// </summary>
        public static void InvalidateEquipmentCache(this IUnitOfWork unitOfWork, Guid projectId)
        {
            _cache.Remove(MemoryCacheService.CacheKeys.EquipmentByProject(projectId));
            _cache.Remove(MemoryCacheService.CacheKeys.EquipmentTagsByProject(projectId));
            Log.Debug("Equipment cache invalidated for project: {ProjectId}", projectId);
        }

        /// <summary>
        /// Invalidates all caches
        /// </summary>
        public static void InvalidateAllCaches(this IUnitOfWork unitOfWork)
        {
            _cache.Clear();
            Log.Information("All caches invalidated");
        }
    }
}
