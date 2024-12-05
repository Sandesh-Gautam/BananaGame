// The ApplicationDbContext class represents the session with the database.
// It allows interaction with the database tables mapped to the User, UserGameRecord, UserHighscore, and UserStreak models.
// By inheriting from DbContext, it provides methods for querying and saving data to the database using Entity Framework Core.
// The constructor takes DbContextOptions to configure the database connection, which is typically provided via dependency injection.
// This class serves as the main context for interacting with the BananaGame database.
using BananaGame.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    // Constructor that passes the DbContextOptions to the base class (DbContext) for configuration
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // DbSets representing tables in the database for Users, Game Records, Highscores, and Streaks
    public DbSet<User> Users { get; set; }
    public DbSet<UserGameRecord> UserGameRecords { get; set; }
    public DbSet<UserHighscore> UserHighscores { get; set; }
    public DbSet<UserStreak> UserStreaks { get; set; }
}
