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
        public DbSet<News> News { get; set; }
        public DbSet<Service> Service { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Topic> Topic { get; set; }
        public DbSet<Profile> Profile { get; set; }
        public DbSet<ServiceTopic> ServiceTopic { get; set; }
        public DbSet<Welcome> Welcome { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // 設定 Test 實體對應到 Test Schema 的 Test Table
            modelBuilder.Entity<Test>()
                .ToTable("Test", schema: "Test");
            
            // 設定 News 實體對應到 Test Schema 的 News Table
            modelBuilder.Entity<News>()
                .ToTable("News", schema: "Test")
                .HasKey(n => n.Id);

            // 設定 Service 實體對應到 Test Schema 的 Service Table
            modelBuilder.Entity<Service>()
                .ToTable("Services", schema: "Test")
                .HasKey(s => s.Name);

            // 設定 Staff 實體對應到 Test Schema 的 Staff Table
            modelBuilder.Entity<Staff>()
                .ToTable("Staff", schema: "Test")
                .HasKey(s => s.Id);

            // 設定 Topic 實體對應到 Test Schema 的 Topic Table
            modelBuilder.Entity<Topic>()
                .ToTable("Topics", schema: "Test")
                .HasKey(t => t.Name);

            // 設定 Profile 實體對應到 Test Schema 的 Profile Table
            modelBuilder.Entity<Profile>()
                .ToTable("CounselorProfiles", schema: "Test")
                .HasKey(p => p.Id);

            // 設定 ServiceTopic 實體對應到 Test Schema 的 ServiceTopic Table
            // 複合主鍵 (ServiceName, TopicName)
            modelBuilder.Entity<ServiceTopic>()
                .ToTable("ServiceTopic", schema: "Test")
                .HasKey(st => new { st.ServiceName, st.TopicName });

            modelBuilder.Entity<Welcome>()
                .ToTable("Welcome", schema: "Test")
                .HasKey(w => w.Id);
        }
    }
}
