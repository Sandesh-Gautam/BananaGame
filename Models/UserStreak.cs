using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    public class UserStreak
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Streak { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
