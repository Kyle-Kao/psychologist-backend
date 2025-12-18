using Microsoft.EntityFrameworkCore;

namespace MyProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Test> Test { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // 設定 Test 實體對應到 Test Schema 的 Test Table
            modelBuilder.Entity<Test>()
                .ToTable("Test", schema: "Test");
        }
    }
}
