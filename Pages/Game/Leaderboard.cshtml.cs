using BananaGame.Data;
using BananaGame.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BananaGame.Pages.Game
{
    [Authorize]
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
            // Fetch the highest score for each user by grouping by UserId
            var topScores = _context.UserGameRecords
                .Include(ugr => ugr.User)  // Include the User entity to access the username
                .GroupBy(ugr => ugr.UserId)  // Group by UserId to get the highest score for each user
                .Select(group => new
                {
                    UserId = group.Key,
                    Username = group.FirstOrDefault().User.Username, // Get the username for the group
                    HighScore = group.Max(ugr => ugr.Score)  // Get the highest score in the group
                })
                .OrderByDescending(x => x.HighScore)  // Order by the highest score
                .Take(10)  // Limit to top 10 users
                .ToList();

            // Create a list of LeaderboardEntry objects with rank, username, and score
            LeaderboardEntries = topScores.Select((entry, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                Username = entry.Username,
                Score = entry.HighScore
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
