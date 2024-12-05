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

namespace BananaGame.Pages
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
        public int HScore { get; set; }

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

       
        public async Task OnGetAsync()
        {
            var userId = await GetLoggedInUserIdAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                
                Lives = user.Lives > 0 ? user.Lives : 3;
            }

            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);
            Streak = userStreak?.Streak ?? 0; 

            if (!GameOver) 
            {
                await FetchNewQuestionAsync(userId);
            }
        }

        public async Task<IActionResult> OnPostSubmitGuessAsync(int guess)
        {
            var userId = await GetLoggedInUserIdAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);
            var userHighscore = await _context.UserHighscores.FirstOrDefaultAsync(us => us.UserId == userId);

            // Load API data from TempData or retain current state
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
                if (guess == SolutionStore.GetValue(userId.ToString()))
                {
                    currentScore += 10;               
                    currentStreak++;
                    Feedback = "Predicted Correctly!";
                    await UpdateUserStreakAsync(userId, currentStreak);
                    await FetchNewQuestionAsync(userId);
                }
                else 
                {
                    currentLives--;
                    currentStreak = 0;
                    Feedback = $"Incorrect. Try again! {currentLives} Free Lives remaining.";

                    await UpdateLivesAsync(userId, currentLives);
                    await UpdateUserStreakAsync(userId, currentStreak);

                    if (currentLives > 0)
                    {
                        // Retain current question and image
                       
                        TempData["QuestionImageUrl"] = QuestionImageUrl;
                        TempData["Solution"] = Solution;
                    }
                    else
                    {
                        // Game over: Keep current image and solution
                        GameOver = true;
                     
                        Feedback = $"Game Over! The correct answer was {Solution}.Your total score is {currentScore} Try again!";

                        // Check if score is greater than the user's high score and update if necessary
                        if (currentScore > (userHighscore?.Score ?? 0))
                        {
                            await UpdateHighScore(userId, currentScore); // Save the high score
                        }

                        // Reset lives and streak for a new game
                        currentLives = 3; // Reset lives to 3
                        currentStreak = 0; // Reset streak

                        await SaveGameRecordAsync(userId, currentScore); // Save game record with total score
                        currentScore = 0;
                        await UpdateLivesAsync(userId, currentLives);
                        await UpdateUserStreakAsync(userId, currentStreak);

                        // Don't fetch a new question if lives are 0
                        // Retain current question image and solution
                        TempData["QuestionImageUrl"] = QuestionImageUrl;
                        TempData["Solution"] = Solution;
                    }
                }
            }    
            Lives = currentLives;
            Streak = currentStreak;
            user.Score = currentScore; 
            await _context.SaveChangesAsync();
            TempData["QuestionImageUrl"] = QuestionImageUrl;
            TempData["Solution"] = Solution;

            return Page();
        }

        private async Task UpdateHighScore(int userId, int score)
        {
            
            var userHighscore = await _context.UserHighscores
                                               .FirstOrDefaultAsync(h => h.UserId == userId);

            if (userHighscore != null)
            {
                
                if (score > userHighscore.Score)
                {
                    userHighscore.Score = score; 
                    await _context.SaveChangesAsync(); 
                }
            }
            else
            {
                userHighscore = new UserHighscore
                {
                    UserId = userId,
                    Score = score
                };
                _context.UserHighscores.Add(userHighscore); 
                await _context.SaveChangesAsync(); 
            }
        }
        private async Task SaveGameRecordAsync(int userId, int TotalScore)
        {
            var gameRecord = new UserGameRecord
            {
                UserId = userId,
                Score = TotalScore,
                DatePlayed = DateTime.Now 
            };

            _context.UserGameRecords.Add(gameRecord); 
            await _context.SaveChangesAsync();
        }

        private async Task InitializeGameAsync(int userId)
        {
            Lives = 3; 
            GameOver = false; 
            Streak = 0;

            await FetchNewQuestionAsync(userId);
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

                TempData["QuestionImageUrl"] = QuestionImageUrl; // Store in TempData
                TempData["Solution"] = Solution; // Store in TempData

                SolutionStore.SetValue(userId.ToString(), Solution);
            }
            else
            {
                QuestionImageUrl = "default-image-url";
                Solution = 0;
            }
        }

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
                userStreak.Streak = Math.Max(0, streak); 
            }

            await _context.SaveChangesAsync();
        }


    
        private async Task UpdateLivesAsync(int userId, int lifelines)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Lives = Math.Max(0, lifelines); 
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