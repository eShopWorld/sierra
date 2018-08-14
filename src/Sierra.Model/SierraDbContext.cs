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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>();
            modelBuilder.Entity<Fork>()
                .HasKey(t => new { t.SourceRepositoryName, t.TenantCode });
        }    
    
        public void AttachSingular(object entity) 
        {
            ChangeTracker.TrackGraph(entity, (node) =>
            {                
                if (node.Entry.Entity != entity)
                    node.Entry.State = EntityState.Detached;
                else
                    node.Entry.State = EntityState.Added;
            });
        }

        public  async Task<Tenant> LoadCompleteTenant(string tenantCode)
        {
            return await Tenants
                .Include(t=>t.CustomSourceRepos)
                .FirstOrDefaultAsync(t => t.Code == tenantCode);
        }
    }
}
