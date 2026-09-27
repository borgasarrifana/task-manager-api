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

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Role).HasDefaultValue(UserRole.Member);

                // 254 = maximum length of an email address (RFC 5321)
                entity.Property(u => u.Email).HasMaxLength(254);

                // Unique among non-null values — Postgres allows many NULLs in a unique index
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Project>()
                .Property(p => p.IsCompleted)
                .HasDefaultValue(false);
        }
    }
}