using BananaGame.Data; // Importing application data models for database interactions
using BananaGame.Models; // Importing models for game logic and storage
using Microsoft.AspNetCore.Authorization; // For handling authorization (user login)
using Microsoft.AspNetCore.Mvc; // MVC framework for handling web requests and responses
using Microsoft.AspNetCore.Mvc.RazorPages; // Razor Pages for serving dynamic pages
using Microsoft.EntityFrameworkCore; // EF Core for database operations
using Newtonsoft.Json; // For JSON deserialization from external APIs
using System; // System utilities for handling dates, exceptions, etc.
using System.Linq; // LINQ queries to manipulate collections
using System.Net.Http; // HTTP client to make requests to external APIs
using System.Security.Claims; // For extracting claims (user identity) from the HTTP context
using System.Threading.Tasks; // Asynchronous programming support

namespace BananaGame.Pages
{
    // This page is only accessible by authorized users (OOP: Authorization/Access Control)
    [Authorize]
    public class GameModel : PageModel
    {
        private readonly HttpClient _httpClient; // Interoperability: used for making HTTP requests to external services
        private readonly ApplicationDbContext _context; // Application's database context, an example of dependency injection and interaction with the DB (OOP principle: Inversion of Control)
        private readonly IHttpContextAccessor _httpContextAccessor; // To access HTTP context and manage user claims (virtual identity)

        // Constructor for injecting dependencies (OOP: Dependency Injection)
        public GameModel(HttpClient httpClient, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient; // Interoperability: HTTP client used for external API requests
            _context = context; // Database context injected for interacting with the database
            _httpContextAccessor = httpContextAccessor; // Access HTTP context to retrieve user identity and other session data
        }

        // Properties that represent the current game state and UI feedback
        public string QuestionImageUrl { get; set; } // Holds the current question (game state)
        public int Solution { get; set; } // Holds the solution (game logic)
        public string Feedback { get; set; } // Provides feedback to the player (event-driven interaction with the UI)
        public bool GameOver { get; set; } // Tracks if the game is over (event-driven)

        public int Lives { get; set; } // The number of lives remaining (virtual identity: User's game state)
        public int Streak { get; set; } // The player's winning streak (game state tied to virtual identity)

        // Retrieves the currently logged-in user's ID (virtual identity) based on claims
        private async Task<int> GetLoggedInUserIdAsync()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier); // Virtual Identity: Extracting user ID from JWT or cookies

            if (userIdClaim == null) // Error handling for missing identity (event-driven)
            {
                throw new Exception("User must login.");
            }

            var userId = int.Parse(userIdClaim.Value); // Parsing user ID from the claim (interoperability: claim-based authentication)

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId); // Retrieving the user object from the database (interoperability: DB interaction)
            if (user == null) // Error handling for non-existent user
            {
                throw new Exception($"User {userId} does not exist.");
            }

            return userId; // Returning the user's ID for further operations (virtual identity)
        }

        // Method to initialize the game state when the page is loaded (event-driven programming: user interaction triggers state change)
        public async Task OnGetAsync()
        {
            var userId = await GetLoggedInUserIdAsync();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                Lives = user.Lives; // Retrieve lives from the database
            }

            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);
            Streak = userStreak?.Streak ?? 0; // Retrieve streak from the database

            if (!GameOver) // Initialize the game only if not already game over
            {
                await FetchNewQuestionAsync(userId);
            }
        }


        //Handles the submission of the player's guess (event-driven: user interaction triggers state changes)
        //public async Task<IActionResult> OnPostSubmitGuessAsync(int guess)
        //{
        //    var userId = await GetLoggedInUserIdAsync();

        //    // Retrieve user and streak data
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        //    var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);

        //    if (user == null)
        //    {
        //        Feedback = "User data not found. Please try again.";
        //        return Page();
        //    }

        //    // Load API data from TempData or retain current state
        //    if (TempData.ContainsKey("QuestionImageUrl") && TempData.ContainsKey("Solution"))
        //    {
        //        QuestionImageUrl = TempData["QuestionImageUrl"]?.ToString();
        //        Solution = int.Parse(TempData["Solution"]?.ToString() ?? "0");
        //    }

        //    int currentLives = user.Lives; // User's current lives
        //    int currentStreak = userStreak?.Streak ?? 0; // User's current streak

        //    if (!GameOver)
        //    {
        //        if (guess == SolutionStore.GetValue(userId.ToString())) // Correct Prediction
        //        {
        //            currentStreak++;
        //            Feedback = "Predicted Correctly!";
        //            await UpdateUserStreakAsync(userId, currentStreak);
        //            await FetchNewQuestionAsync(userId); // Fetch a new question for correct guess
        //        }
        //        else // Incorrect Prediction
        //        {
        //            currentLives--;
        //            currentStreak = Math.Max(0, currentStreak - 1); // Ensure streak doesn't go negative
        //            Feedback = $"Incorrect. Try again! {currentLives} Free Lives remaining.";

        //            await UpdateLivesAsync(userId, currentLives);
        //            await UpdateUserStreakAsync(userId, currentStreak);

        //            if (currentLives > 0)
        //            {
        //                // Retain current question and image
        //                TempData["QuestionImageUrl"] = QuestionImageUrl;
        //                TempData["Solution"] = Solution;
        //            }
        //            else
        //            {
        //                // Game over: Reset and start a new game
        //                TempData["QuestionImageUrl"] = QuestionImageUrl;
        //                TempData["Solution"] = Solution;
        //                GameOver = true;
        //                Feedback = $"Game Over! The correct answer was {Solution}. Starting a new game.";
        //                currentLives = 5; // Reset lives
        //                currentStreak = 0;

        //                await SaveGameRecordAsync(userId, currentStreak);
        //                await UpdateLivesAsync(userId, currentLives);
        //                await UpdateUserStreakAsync(userId, currentStreak);

        //                await FetchNewQuestionAsync(userId); // Fetch new question for a new game
        //            }
        //        }
        //    }

        //    // Persist updated game state
        //    Lives = currentLives;
        //    Streak = currentStreak;

        //    TempData["QuestionImageUrl"] = QuestionImageUrl;
        //    TempData["Solution"] = Solution;

        //    return Page();
        //}

        public async Task<IActionResult> OnPostSubmitGuessAsync(int guess)
        {
            var userId = await GetLoggedInUserIdAsync();

            // Retrieve user and streak data
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var userStreak = await _context.UserStreaks.FirstOrDefaultAsync(us => us.UserId == userId);

            if (user == null)
            {
                Feedback = "User data not found. Please try again.";
                return Page();
            }

            // Load API data from TempData or retain current state
            if (TempData.ContainsKey("QuestionImageUrl") && TempData.ContainsKey("Solution"))
            {
                QuestionImageUrl = TempData["QuestionImageUrl"]?.ToString();
                Solution = int.Parse(TempData["Solution"]?.ToString() ?? "0");
            }

            int currentLives = user.Lives; // User's current lives
            int currentStreak = userStreak?.Streak ?? 0; // User's current streak

            if (!GameOver)
            {
                if (guess == SolutionStore.GetValue(userId.ToString())) // Correct Prediction
                {
                    currentStreak++;
                    Feedback = "Predicted Correctly!";
                    await UpdateUserStreakAsync(userId, currentStreak);
                    await FetchNewQuestionAsync(userId); // Fetch a new question for correct guess
                }
                else // Incorrect Prediction
                {
                    currentLives--;
                    currentStreak = Math.Max(0, currentStreak - 1); // Ensure streak doesn't go negative
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
                        Feedback = $"Game Over! The correct answer was {Solution}. Try again!";

                        // Reset lives and streak for a new game
                        currentLives = 5; // Reset lives
                        currentStreak = 0; // Reset streak

                        await SaveGameRecordAsync(userId, currentStreak);
                        await UpdateLivesAsync(userId, currentLives);
                        await UpdateUserStreakAsync(userId, currentStreak);

                        // Don't fetch a new question if lives are 0
                        // Remove the new question fetch logic to prevent image and solution change
                        TempData["QuestionImageUrl"] = QuestionImageUrl;  // Retain the same image
                        TempData["Solution"] = Solution;  // Retain the same solution
                    }
                }
            }

            // Persist updated game state
            Lives = currentLives;
            Streak = currentStreak;

            // Ensure the image and solution persist even after game over
            TempData["QuestionImageUrl"] = QuestionImageUrl;
            TempData["Solution"] = Solution;

            return Page();
        }







        // Updates the user's high score (game state change: event-driven)
        private async Task UpdateHighScoreAsync(int userId, int streak)
        {
            var userHighScore = await _context.UserHighscores.FirstOrDefaultAsync(u => u.UserId == userId); // Retrieve the user's high score from DB (interoperability: DB interaction)

            if (userHighScore == null) // If no high score is found, create a new entry
            {
                userHighScore = new UserHighscore
                {
                    UserId = userId,
                    Score = streak
                };
                _context.UserHighscores.Add(userHighScore); // Add new high score entry (event-driven: state persistence)
            }
            else
            {
                if (streak > userHighScore.Score) // If the new streak is higher than the current high score, update it
                {
                    userHighScore.Score = streak;
                }
            }

            await _context.SaveChangesAsync(); // Persist the changes (event-driven)
        }

        // Saves the game record with the total score (game state persistence)
        private async Task SaveGameRecordAsync(int userId, int TotalScore)
        {
            var gameRecord = new UserGameRecord
            {
                UserId = userId,
                Score = TotalScore,
                DatePlayed = DateTime.Now // Date of the game session (event-driven: state persistence)
            };

            _context.UserGameRecords.Add(gameRecord); // Add the record to the database (event-driven)
            await _context.SaveChangesAsync(); // Persist changes to DB (event-driven)
        }

        // Initializes the game (sets the starting state, event-driven interaction with game logic)
        private async Task InitializeGameAsync(int userId)
        {
            Lives = 5; // Set initial lives
            GameOver = false; // Reset game over flag
            Streak = 0; // Reset streak

            await FetchNewQuestionAsync(userId); // Fetch the first question
        }

        // Fetches a new question from the external API (interoperability: external service)
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

        // Updates the user's winning streak (game state persistence)
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
                userStreak.Streak = Math.Max(0, streak); // Ensure streak doesn't go below 0
            }

            await _context.SaveChangesAsync();
        }


        // Updates the user's remaining lives (game state change)
        private async Task UpdateLivesAsync(int userId, int lifelines)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Lives = Math.Max(0, lifelines); // Ensure lives don't go below 0
                await _context.SaveChangesAsync();
            }
        }

        // Inner class to deserialize API responses (OOP: encapsulating response structure)
        public class ApiResponse
        {
            public string Question { get; set; } // Question received from the API (game state)
            public int Solution { get; set; } // Solution received from the API (game logic)
        }
    }
}