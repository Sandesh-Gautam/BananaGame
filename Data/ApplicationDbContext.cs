using BananaGame.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BananaGame.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<UserGameRecord> UserGameRecords { get; set; }
        public DbSet<UserHighscore> UserHighscores { get; set; }
        public DbSet<UserStreak> UserStreaks { get; set; }

    }
}
