using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotAlone.Models
{
    /// <summary>
    /// Represents a completed game session with summary data
    /// </summary>
    public class GameHistory
    {
        /// <summary>
        /// Unique identifier for this game history record
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Foreign key to the User who played this game
        /// </summary>
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property: The user who played this game
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Date and time when the game was completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total rounds played in this game
        /// </summary>
        public int RoundsPlayed { get; set; }

        /// <summary>
        /// Player's final progress (0-7, where 7 is win)
        /// </summary>
        public int PlayerProgress { get; set; }

        /// <summary>
        /// Creature's final progress (0-5, where 5 is win)
        /// </summary>
        public int CreatureProgress { get; set; }

        /// <summary>
        /// Game result: Win = player reached 7, Loss = creature reached 5 or player willpower <= 0
        /// </summary>
        [StringLength(50)]
        public string Result { get; set; } = "Loss"; // Win or Loss

        /// <summary>
        /// Duration of the game in seconds
        /// </summary>
        public int DurationSeconds { get; set; }
    }
}
