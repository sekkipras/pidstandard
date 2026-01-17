using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PIDStandardization.Data.Configuration;

namespace PIDStandardization.Data.Context
{
    /// <summary>
    /// Design-time factory for creating PIDDbContext during migrations
    /// </summary>
    public class PIDDbContextFactory : IDesignTimeDbContextFactory<PIDDbContext>
    {
        public PIDDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PIDDbContext>();

            // Use the connection string from DatabaseConfiguration
            var config = new DatabaseConfiguration();
            optionsBuilder.UseSqlServer(config.ConnectionString);

            return new PIDDbContext(optionsBuilder.Options);
        }
    }
}
