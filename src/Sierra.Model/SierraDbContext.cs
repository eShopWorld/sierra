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
        public DbSet<SourceCodeRepository> SourceCodeRepositories { get; set; }
        public DbSet<VstsBuildDefinition> BuildDefinitions { get; set; }
        public DbSet<VstsReleaseDefinition> ReleaseDefinitions { get; set; }

        public DbSet<ResourceGroup> ResourceGroups { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>();
            modelBuilder.Entity<VstsBuildDefinition>();
            modelBuilder.Entity<SourceCodeRepository>();                
            modelBuilder.Entity<VstsBuildDefinition>()
                .HasOne<Tenant>()
                .WithMany(t => t.BuildDefinitions)
                .HasForeignKey(t => t.TenantCode)
                .OnDelete(DeleteBehavior.Restrict); //this is required to avoid delete cascade loop 

            modelBuilder.Entity<VstsReleaseDefinition>()
                .HasOne<Tenant>()
                .WithMany(t => t.ReleaseDefinitions)
                .HasForeignKey(t => t.TenantCode)
                .OnDelete(DeleteBehavior.Restrict); //this is required to avoid delete cascade loop 

            modelBuilder.Entity<VstsReleaseDefinition>()
                .HasOne<Tenant>()
                .WithMany(t => t.ReleaseDefinitions)
                .HasForeignKey(t => t.TenantCode);
        }

        /// <summary>
        /// loads complete tenant based on the tenant code with all dependent models
        /// </summary>
        /// <param name="tenantCode">tenant code</param>
        /// <returns>tenant structure</returns>
        public async Task<Tenant> LoadCompleteTenantAsync(string tenantCode)
        {
            return await Tenants
                .Include(t=>t.SourceRepos)
                .Include(t=>t.BuildDefinitions)
                .Include(t=>t.ReleaseDefinitions)
                    .FirstOrDefaultAsync(t => t.Code == tenantCode);
        }
    }
}
