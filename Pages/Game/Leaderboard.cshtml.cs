using BananaGame.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BananaGame.Pages.Game
{
    [Authorize] // Ensure only authenticated users can access the leaderboard page
    public class LeaderboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LeaderboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // This property holds the list of leaderboard entries, containing rank, username, and score
        public List<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();

        // This method is triggered when the page is accessed, and it fetches the top 10 high scores
        public void OnGet()
        {
            // Query the database to fetch the highest score for each user
            var topScores = _context.UserGameRecords
                .Include(ugr => ugr.User)  // Include the User entity to access the username
                .GroupBy(ugr => ugr.UserId)  // Group records by UserId to calculate the highest score per user
                .Select(group => new
                {
                    UserId = group.Key,
                    Username = group.FirstOrDefault().User.Username, // Select the username of the user
                    HighScore = group.Max(ugr => ugr.Score)  // Find the maximum score for each user
                })
                .OrderByDescending(x => x.HighScore)  // Order users by their highest score in descending order
                .Take(10)  // Limit the result to the top 10 scores
                .ToList();

            // Map the top scores to a list of LeaderboardEntry objects with rank, username, and score
            LeaderboardEntries = topScores.Select((entry, index) => new LeaderboardEntry
            {
                Rank = index + 1,  // Set the rank as index + 1 (1-based index)
                Username = entry.Username,  // Assign the username
                Score = entry.HighScore  // Assign the high score
            }).ToList();
        }

        // A model class to hold the leaderboard data for rendering
        public class LeaderboardEntry
        {
            public int Rank { get; set; }  // User's rank based on their high score
            public string Username { get; set; }  // User's username
            public int Score { get; set; }  // User's highest score
        }
    }
}
