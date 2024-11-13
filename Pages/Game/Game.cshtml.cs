using BananaGame.Data;
using BananaGame.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BananaGame.Pages.Game
{
    [Authorize]
    public class GameModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GameModel(HttpClient httpClient, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public string QuestionImageUrl { get; set; }
        public int Solution { get; set; }
        public string Feedback { get; set; }
        public bool GameOver { get; set; }
        public int Lives { get; set; }
        public int Streak { get; set; }

        private async Task<int> GetLoggedInUserIdAsync()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                throw new Exception("User is not logged in.");
            }

            var userId = int.Parse(userIdClaim.Value);

            // Check if the user exists in the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception($"User with ID {userId} does not exist in the database.");
            }

            return userId;
        }

        public async Task OnGetAsync()
        {
            var userId = await GetLoggedInUserIdAsync();

            // Retrieve Lives and Streak from the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Lives = user?.Lives ?? 3; // Default lives to 3 if null

            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(u => u.UserId == userId);
            Streak = userStreak?.Streak ?? 0;

            await InitializeGameAsync(userId);
        }

        public async Task<IActionResult> OnPostSubmitGuessAsync(int guess)
        {
            var userId = await GetLoggedInUserIdAsync();

            if (!GameOver)
            {
                Console.WriteLine($"Guess: {guess}, Solution: {Solution}");

                if (guess == Solution)
                {
                    // Correct guess
                    Streak++;
                    Feedback = "Correct!";
                    await UpdateUserStreakAsync(userId, Streak);

                    // Reset lives to 3 after a correct guess
                    Lives = 3;
                    await UpdateLivesAsync(userId, Lives);
                }
                else
                {
                    // Incorrect guess
                    Lives--;
                    Feedback = $"Incorrect. Try again! Lives left: {Lives}";

                    // Update lives in the database
                    await UpdateLivesAsync(userId, Lives);

                    if (Lives == 0)
                    {
                        GameOver = true;
                        Feedback = $"Game Over! The correct answer was {Solution}. Your final score is {Streak}. Play again?";
                        Lives = 3; // Reset lives for the next game
                    }
                }
            }

            return Page(); // Return the page with feedback and updated data
        }

        private async Task UpdateHighScoreAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception("User does not exist in the database.");
            }

            var userHighscore = await _context.UserHighscores.FirstOrDefaultAsync(u => u.UserId == userId);

            if (userHighscore == null)
            {
                _context.UserHighscores.Add(new UserHighscore
                {
                    UserId = userId,
                    Score = Streak
                });
            }
            else if (Streak > userHighscore.Score)
            {
                userHighscore.Score = Streak;
            }

            await _context.SaveChangesAsync();
        }

        private async Task SaveGameRecordAsync(int userId)
        {
            var gameRecord = new UserGameRecord
            {
                UserId = userId,
                Score = Streak,
                DatePlayed = DateTime.UtcNow
            };

            _context.UserGameRecords.Add(gameRecord);
            await _context.SaveChangesAsync();
        }

        private async Task InitializeGameAsync(int userId)
        {
            Lives = 3;
            GameOver = false;

            // Fetch a new question for the game
            await FetchNewQuestionAsync(userId);

            // Load the user's previous streak if it exists
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(u => u.UserId == userId);

            Streak = userStreak?.Streak ?? 0;
        }

        private async Task FetchNewQuestionAsync(int userId)
        {
            var apiUrl = "https://marcconrad.com/uob/banana/api.php";
            var response = await _httpClient.GetStringAsync(apiUrl);
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

            if (apiResponse != null)
            {
                QuestionImageUrl = apiResponse.Question;
                Solution = apiResponse.Solution;
            }
            else
            {
                QuestionImageUrl = "default-image-url";
                Solution = 0;
                Console.WriteLine("API response was invalid, using default values.");
            }
        }

        private async Task UpdateUserStreakAsync(int userId, int streak)
        {
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(u => u.UserId == userId);

            if (userStreak == null)
            {
                userStreak = new UserStreak
                {
                    UserId = userId,
                    Streak = streak
                };
                _context.UserStreaks.Add(userStreak);
            }
            else
            {
                userStreak.Streak = streak;
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateLivesAsync(int userId, int lives)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Lives = lives;
                await _context.SaveChangesAsync();
            }
        }

        public class ApiResponse
        {
            public string Question { get; set; }
            public int Solution { get; set; }
        }
    }
}
