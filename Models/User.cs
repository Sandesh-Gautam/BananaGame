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

        public int Score {  get; set; }

        public int Lives { get; set; } = 3;

        public UserHighscore Highscore { get; set; }

        public List<UserGameRecord> GameRecords { get; set; }
        public UserStreak Streak { get; set; }

    }
}
