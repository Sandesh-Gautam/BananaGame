using System;
using System.ComponentModel.DataAnnotations;

namespace BananaGame.Models
{
    public class UserGameRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Score { get; set; }

        [Required]
        public DateTime DatePlayed { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
