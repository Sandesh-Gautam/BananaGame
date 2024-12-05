using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    // The UserStreak class represents a user's current win streak in the BananaGame.
    public class UserStreak
    {
        // The Id property is the primary key for the UserStreak table in the database.
        [Key]
        public int Id { get; set; }

        // Streak represents the number of consecutive wins by the user.
        // This field is marked as required because every streak must have a value.
        [Required]
        public int Streak { get; set; }

        // UserId is a foreign key linking the streak to the specific user who has the streak.
        public int UserId { get; set; }

        // User property is a navigation property that allows access to the associated User entity.
        public User User { get; set; }
    }
}
