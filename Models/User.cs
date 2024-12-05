using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    // Enum to define user roles, currently only includes 'User' role.
    public enum UserRole
    {
        User
    }

    // The User class represents a player in the BananaGame with essential attributes and relationships.
    public class User
    {
        // The Id property is the primary key for the User table in the database.
        [Key]
        public int Id { get; set; }

        // Fullname is a required field representing the user's full name.
        [Required]
        public string Fullname { get; set; }

        // Username is a required field used for user login and identification.
        [Required]
        public string Username { get; set; }

        // Password is a required field to authenticate the user.
        [Required]
        public string Password { get; set; }

        // Score represents the current score of the player in the game.
        public int Score { get; set; }

        // Lives tracks the number of lives the user has. Default value is 3.
        public int Lives { get; set; } = 3;

        // Highscore represents the user's best score achieved in the game.
        public UserHighscore Highscore { get; set; }

        // GameRecords holds a list of UserGameRecord objects representing each game session played by the user.
        public List<UserGameRecord> GameRecords { get; set; }

        // Streak tracks the user's current win streak.
        public UserStreak Streak { get; set; }
    }
}
