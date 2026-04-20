using System.ComponentModel.DataAnnotations;

namespace NotAlone.Models
{
    /// <summary>
    /// Represents a registered user in the application
    /// Simplified MVP model without ASP.NET Identity framework
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique user identifier (GUID primary key)
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User's email address (unique, required)
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's username (unique, required)
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password (required, uses BCrypt)
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// When the user registered
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property: User's completed games
        /// </summary>
        public virtual ICollection<GameHistory> GameHistories { get; set; } = new List<GameHistory>();
    }
}
