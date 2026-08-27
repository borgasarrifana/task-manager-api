using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Models;

namespace TaskManager.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasDefaultValue(UserRole.Member);

            modelBuilder.Entity<Project>()
                .Property(p => p.IsCompleted)
                .HasDefaultValue(false);
        }
    }
}