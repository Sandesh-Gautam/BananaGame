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
using System.Security.Claims;
using System.Threading.Tasks;

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
        // This method retrieves the logged-in user's ID from claims
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

            // Fetch the user data
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                // Set the user's lives to 3
                user.Lives = 3;

                // Save changes to the Users table
                await _context.SaveChangesAsync();
            }

            // Fetch UserStreak separately based on userId
            var userStreak = await _context.UserStreaks
                .FirstOrDefaultAsync(us => us.UserId == userId);

            // Assign Streak value from userStreak or default to 0 if no streak exists
            Streak = userStreak?.Streak ?? 0;

            // Initialize game settings
            await InitializeGameAsync(userId);
        }



        // OnPostSubmitGuessAsync handles the user submitting a guess
        public async Task<IActionResult> OnPostSubmitGuessAsync(int guess)
        {
            var userId = await GetLoggedInUserIdAsync();

            if (!GameOver)
            {
                if (guess == SolutionStore.GetValue(userId.ToString()))
                {
                    int score = _context.UserStreaks.FirstOrDefault(us => us.UserId == userId)?.Streak ?? 0;
                    score++;
                    Feedback = "Correct!";
                    await UpdateUserStreakAsync(userId, score);

                    await UpdateHighScoreAsync(userId, score);
                    await UpdateLivesAsync(userId, Lives);

                    // Fetch the next question after the correct answer
                    await FetchNewQuestionAsync(userId);
                }
                else
                {
                    int lifes = _context.Users.FirstOrDefault(u => u.Id == userId)?.Lives ?? 0;
                    lifes--;
                    Feedback = $"Incorrect. Try again! Lives left: {lifes}";

                    // Reset score if incorrect
                    int score = _context.UserStreaks.FirstOrDefault(us => us.UserId == userId)?.Streak ?? 0;
                    score = 0;

                    await UpdateUserStreakAsync(userId, score);

                    // Update lives in the database
                    await UpdateLivesAsync(userId, lifes);

                    // Keep the same question and image on incorrect guess
                    await FetchNewQuestionAsync(userId);  // Re-fetch the same question

                    if (lifes == 0)
                    {
                        GameOver = true;
                        Feedback = $"Game Over! The correct answer was {Solution}. Your final score is {Streak}. Play again?";

                        // Save the game record after the game is over
                        await SaveGameRecordAsync(userId, Streak);

                        // Update high score at the end of the game (game over scenario)
                        await UpdateHighScoreAsync(userId, Streak);
                    }
                }
            }

            // Return to the page to refresh the game state and feedback
            return Page();
        }

        private async Task UpdateHighScoreAsync(int userId, int streak)
        {
            // Find the user's existing high score record
            var userHighScore = await _context.UserHighscores.FirstOrDefaultAsync(u => u.UserId == userId);

            if (userHighScore == null)
            {
                // If no high score exists, create a new one
                userHighScore = new UserHighscore
                {
                    UserId = userId,
                    Score = streak
                };
                _context.UserHighscores.Add(userHighScore);
            }
            else
            {
                // If the current streak is higher than the stored high score, update the high score
                if (streak > userHighScore.Score)
                {
                    userHighScore.Score = streak;
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
        }

      
        private async Task SaveGameRecordAsync(int userId, int score)
        {
            var gameRecord = new UserGameRecord
            {
                UserId = userId,
                Score = score,
                DatePlayed = DateTime.Now
            };

            _context.UserGameRecords.Add(gameRecord);
            await _context.SaveChangesAsync();
        }





        // InitializeGameAsync initializes game settings
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

        // FetchNewQuestionAsync retrieves a new question from an API
        private async Task FetchNewQuestionAsync(int userId)
        {
            var apiUrl = "https://marcconrad.com/uob/banana/api.php";
            var response = await _httpClient.GetStringAsync(apiUrl);
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

            if (apiResponse != null)
            {
                QuestionImageUrl = apiResponse.Question;
                Solution = apiResponse.Solution;
                SolutionStore.SetValue(userId.ToString(), Solution);
            }
            else
            {
                QuestionImageUrl = "default-image-url";
                Solution = 0;
               
            }
        }

        // UpdateUserStreakAsync updates the user's streak in the database
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

        // UpdateLivesAsync updates the user's remaining lives
        private async Task UpdateLivesAsync(int userId, int lives)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Lives = lives;
                await _context.SaveChangesAsync();
            }
        }

        // ApiResponse class to handle the API response format
        public class ApiResponse
        {
            public string Question { get; set; }
            public int Solution { get; set; }
        }
    }
}
