namespace Sierra.Model
{
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;

    /// <summary>
    /// EF.Core Db Context implementation for Sierra State Management
    /// </summary>
    public class SierraDbContext :DbContext
    {
        public string ConnectionString { get; set; } = "Data Source=.;Initial Catalog=Sierra;Integrated Security=True"; //for local dev experience

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Fork> Forks { get; set; }
        public DbSet<BuildDefinition> BuildDefinitions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>();
            modelBuilder.Entity<Fork>()
                .HasKey(t => new { t.SourceRepositoryName, t.TenantCode });
            modelBuilder.Entity<BuildDefinition>();
        }    

        /// <summary>
        /// loads complete tenant based on the tenant code with all dependent models
        /// </summary>
        /// <param name="tenantCode">tenant code</param>
        /// <returns>tenant structure</returns>
        public  async Task<Tenant> LoadCompleteTenantAsync(string tenantCode)
        {
            return await Tenants
                .Include(t=>t.CustomSourceRepos)
                .FirstOrDefaultAsync(t => t.Code == tenantCode);
        }
    }
}
