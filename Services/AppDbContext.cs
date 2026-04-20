using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotAlone.Models;

namespace NotAlone.Services
{
    /// <summary>
    /// Entity Framework Core DbContext for NotAlone application
    /// Handles all database operations for User, GameHistory, GameSession, and Identity entities
    /// </summary>
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        /// <summary>
        /// DbSet for game history records
        /// </summary>
        public DbSet<GameHistory> GameHistories { get; set; }

        /// <summary>
        /// DbSet for active/completed game sessions
        /// </summary>
        public DbSet<GameSession> GameSessions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure GameHistory entity
            modelBuilder.Entity<GameHistory>()
                .HasOne(gh => gh.User)
                .WithMany(u => u.GameHistories)
                .HasForeignKey(gh => gh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GameSession entity
            modelBuilder.Entity<GameSession>()
                .HasKey(gs => gs.Id);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasMany(u => u.GameHistories)
                .WithOne(gh => gh.User)
                .HasForeignKey(gh => gh.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
