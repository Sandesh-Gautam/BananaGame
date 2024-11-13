using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    public class UserHighscore
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Score { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
