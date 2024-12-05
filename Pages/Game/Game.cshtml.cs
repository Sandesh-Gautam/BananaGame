
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

namespace BananaGame.Pages
{
    // The GameModel class represents the logic behind the BananaGame's gameplay page.
    // It handles user interaction, game state management, and database updates during the game.
    [Authorize]
    public class GameModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor to inject dependencies: HttpClient, DbContext, and HttpContextAccessor
        public GameModel(HttpClient httpClient, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Properties representing the current state of the game (question, feedback, lives, streak, etc.)
        public string QuestionImageUrl { get; set; }
        public int Solution { get; set; }
        public string Feedback { get; set; }
        public bool GameOver { get; set; }
        public int Lives { get; set; }
        public int Streak { get; set; }
        public int HScore { get; set; }

        // Method to get the logged-in user's ID from the claim
        private async Task<int> GetLoggedInUserIdAsync()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                throw new Exception("User must login.");
            }

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception($"User {userId} does not exist.");
            }

            return userId;
        }

        // The main method called when the page loads to set up initial game state (lives, streak, etc.)
        public async Task OnGetAsync()
        {
            var userId = await GetLoggedInUserIdAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                Lives = user.Lives > 0 ? user.Lives : 3; // If no lives are left, reset to 3
            }

            // Fetch the user's current streak
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);
            Streak = userStreak?.Streak ?? 0;

            // Fetch a new question if the game is not over
            if (!GameOver)
            {
                await FetchNewQuestionAsync(userId);
            }
        }

        // Method to handle the user's guess submission and update the game state
        public async Task<IActionResult> OnPostSubmitGuessAsync(int guess)
        {
            var userId = await GetLoggedInUserIdAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);
            var userHighscore = await _context.UserHighscores.FirstOrDefaultAsync(us => us.UserId == userId);

            // Load previous question and solution from TempData if they exist
            if (TempData.ContainsKey("QuestionImageUrl") && TempData.ContainsKey("Solution"))
            {
                QuestionImageUrl = TempData["QuestionImageUrl"]?.ToString();
                Solution = int.Parse(TempData["Solution"]?.ToString() ?? "0");
            }

            int currentLives = user.Lives;
            int currentStreak = userStreak?.Streak ?? 0;
            int currentScore = user.Score;

            if (!GameOver)
            {
                // Check if the guess is correct
                if (guess == SolutionStore.GetValue(userId.ToString()))
                {
                    currentScore += 10; // Increase score for a correct guess
                    currentStreak++; // Increment streak
                    Feedback = "Predicted Correctly!"; // Provide feedback
                    await UpdateUserStreakAsync(userId, currentStreak); // Update the user's streak in the database
                    await FetchNewQuestionAsync(userId); // Fetch a new question
                }
                else
                {
                    currentLives--; // Decrease lives for an incorrect guess
                    currentStreak = 0; // Reset streak
                    Feedback = $"Incorrect. Try again! {currentLives} Free Lives remaining."; // Provide feedback

                    await UpdateLivesAsync(userId, currentLives); // Update lives in the database
                    await UpdateUserStreakAsync(userId, currentStreak); // Reset streak in the database

                    // If lives are remaining, keep the current question for the user to retry
                    if (currentLives > 0)
                    {
                        TempData["QuestionImageUrl"] = QuestionImageUrl;
                        TempData["Solution"] = Solution;
                    }
                    else
                    {
                        // Game over logic
                        GameOver = true;
                        Feedback = $"Game Over! The correct answer was {Solution}. Your total score is {currentScore}. Try again!";

                        // Update the high score if the user's current score is higher
                        if (currentScore > (userHighscore?.Score ?? 0))
                        {
                            await UpdateHighScore(userId, currentScore); // Save the new high score
                        }

                        // Reset lives and streak for a new game
                        currentLives = 3;
                        currentStreak = 0;

                        await SaveGameRecordAsync(userId, currentScore); // Save the game record with the total score
                        currentScore = 0; // Reset the score for the next round
                        await UpdateLivesAsync(userId, currentLives); // Update lives in the database
                        await UpdateUserStreakAsync(userId, currentStreak); // Reset streak in the database

                        // Store the question for reference if the game is over
                        TempData["QuestionImageUrl"] = QuestionImageUrl;
                        TempData["Solution"] = Solution;
                    }
                }
            }
            Lives = currentLives;
            Streak = currentStreak;
            user.Score = currentScore;
            await _context.SaveChangesAsync(); // Save changes to the database
            TempData["QuestionImageUrl"] = QuestionImageUrl;
            TempData["Solution"] = Solution;

            return Page();
        }

        // Method to update the user's high score in the database
        private async Task UpdateHighScore(int userId, int score)
        {
            var userHighscore = await _context.UserHighscores
                                               .FirstOrDefaultAsync(h => h.UserId == userId);

            if (userHighscore != null)
            {
                if (score > userHighscore.Score)
                {
                    userHighscore.Score = score; // Update high score if new score is higher
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // If no high score exists, create a new one
                userHighscore = new UserHighscore
                {
                    UserId = userId,
                    Score = score
                };
                _context.UserHighscores.Add(userHighscore);
                await _context.SaveChangesAsync();
            }
        }

        // Method to save the game record to the database after the game ends
        private async Task SaveGameRecordAsync(int userId, int TotalScore)
        {
            var gameRecord = new UserGameRecord
            {
                UserId = userId,
                Score = TotalScore,
                DatePlayed = DateTime.Now
            };

            _context.UserGameRecords.Add(gameRecord); // Add the game record to the database
            await _context.SaveChangesAsync(); // Save changes to the database
        }

        // Fetches a new question from the API
        private async Task FetchNewQuestionAsync(int userId)
        {
            var apiUrl = "https://marcconrad.com/uob/banana/api.php";
            var response = await _httpClient.GetStringAsync(apiUrl);
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

            if (apiResponse != null)
            {
                QuestionImageUrl = apiResponse.Question;
                Solution = apiResponse.Solution;

                TempData["QuestionImageUrl"] = QuestionImageUrl; // Store question URL in TempData
                TempData["Solution"] = Solution; // Store solution in TempData

                SolutionStore.SetValue(userId.ToString(), Solution); // Store solution for later validation
            }
            else
            {
                QuestionImageUrl = "default-image-url"; // Fallback in case of API failure
                Solution = 0;
            }
        }

        // Updates the user's streak in the database
        private async Task UpdateUserStreakAsync(int userId, int streak)
        {
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);
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
                userStreak.Streak = Math.Max(0, streak); // Ensure streak is never negative
            }

            await _context.SaveChangesAsync(); // Save changes to the database
        }

        // Updates the user's remaining lives
        private async Task UpdateLivesAsync(int userId, int lifelines)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Lives = Math.Max(0, lifelines); // Ensure lives don't go negative
                await _context.SaveChangesAsync(); // Save changes to the database
            }
        }

        // Nested class to represent the API response structure
        public class ApiResponse
        {
            public string Question { get; set; }
            public int Solution { get; set; }
        }
    }
}
