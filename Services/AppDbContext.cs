using Microsoft.EntityFrameworkCore;
using NotAlone.Models;

namespace NotAlone.Services
{
    /// <summary>
    /// Entity Framework Core DbContext for NotAlone application
    /// Handles all database operations for User, GameHistory, and GameSession entities
    /// MVP: Simplified to 3 core tables (no ASP.NET Identity framework)
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// DbSet for application users
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// DbSet for completed game records
        /// </summary>
        public DbSet<GameHistory> GameHistories { get; set; }

        /// <summary>
        /// DbSet for active/in-progress game sessions
        /// </summary>
        public DbSet<GameSession> GameSessions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure GameHistory entity
            modelBuilder.Entity<GameHistory>()
                .HasOne(gh => gh.User)
                .WithMany(u => u.GameHistories)
                .HasForeignKey(gh => gh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GameSession entity
            modelBuilder.Entity<GameSession>()
                .HasKey(gs => gs.Id);
        }
    }
}
