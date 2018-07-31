namespace Sierra.Model
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// EF.Core Db Context implementation for Sierra State Management
    /// </summary>
    public class SierraDbContext :DbContext
    {
        public string ConnectionString { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>();
            modelBuilder.Entity<Fork>()
                .HasKey(nameof(Fork.SourceRepositoryName), nameof(Fork.TenantName));
        }
    }
}
