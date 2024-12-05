using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    // The UserGameRecord class represents a single game session played by a user.
    public class UserGameRecord
    {
        // The Id property is the primary key for the UserGameRecord table in the database.
        [Key]
        public int Id { get; set; }

        // Score represents the score achieved by the user in this specific game session.
        [Required]
        public int Score { get; set; }

        // DatePlayed records the date and time when the game session was played.
        [Required]
        public DateTime DatePlayed { get; set; }

        // UserId is a foreign key linking the record to the specific user who played the game.
        public int UserId { get; set; }

        // User property is a navigation property that provides access to the associated User entity.
        public User User { get; set; }
    }
}
