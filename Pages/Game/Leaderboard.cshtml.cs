using BananaGame.Data;
using BananaGame.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace BananaGame.Pages.Game
{
    public class LeaderboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LeaderboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // This will hold the leaderboard's list of high scores along with the username and rank
        public List<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();

        // This method will fetch the highest scores and corresponding usernames
        public void OnGet()
        {
            // Fetch top scores from UserGameRecord and include the User information
            var topScores = _context.UserGameRecords
                .Include(ugr => ugr.User)  // Include the User entity to access the username
                .OrderByDescending(ugr => ugr.Score)  // Order by score descending
                .Take(10)  // Limit to top 10 scores
                .ToList();

            // Create a list of LeaderboardEntry objects with rank, username, and score
            LeaderboardEntries = topScores.Select((ugr, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                Username = ugr.User.Username,
                Score = ugr.Score
            }).ToList();
        }

        // A model class to hold the leaderboard data
        public class LeaderboardEntry
        {
            public int Rank { get; set; }
            public string Username { get; set; }
            public int Score { get; set; }
        }
    }
}
