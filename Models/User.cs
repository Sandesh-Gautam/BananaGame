using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    public enum UserRole
    {
        User 
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Fullname { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public int Lives { get; set; } = 3; 

        // Default role for new users will be "User"
        [Required]
        public UserRole Role { get; set; } = UserRole.User; // Default role is set to User

        public List<UserHighscore> Highscores { get; set; }

        public List<UserGameRecord> GameRecords { get; set; }
        public UserStreak Streak { get; set; }

    }
}
