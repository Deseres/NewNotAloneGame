using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NotAlone.Models
{
    /// <summary>
    /// Represents a registered user in the application
    /// Inherits from IdentityUser for built-in authentication
    /// </summary>
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// User's display name
        /// </summary>
        [StringLength(256)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// When the user registered
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property: User's game history
        /// </summary>
        public virtual ICollection<GameHistory> GameHistories { get; set; } = new List<GameHistory>();
    }
}
