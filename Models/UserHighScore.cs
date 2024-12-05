using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    // The UserHighscore class represents the highest score achieved by a user in the BananaGame.
    public class UserHighscore
    {
        // The Id property is the primary key for the UserHighscore table in the database.
        [Key]
        public int Id { get; set; }

        // Score represents the highest score achieved by the user.
        // This field is marked as required because every high score must have a value.
        [Required]
        public int Score { get; set; }

        // UserId is a foreign key linking the high score to the specific user who achieved it.
        public int UserId { get; set; }

        // User property is a navigation property that allows access to the associated User entity.
        public User User { get; set; }
    }
}
