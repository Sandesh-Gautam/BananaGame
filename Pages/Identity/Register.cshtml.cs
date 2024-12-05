using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using BananaGame.Models;
using BananaGame.Services;

namespace BananaGame.Pages.Identity
{
    // PageModel for handling user registration functionality
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        // Constructor to inject ApplicationDbContext (for database access) and JwtService (for JWT token generation)
        public RegisterModel(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // Property for binding the registration form input data
        [BindProperty]
        public RegistrationInput Input { get; set; }

        // Message to show user feedback during registration process
        public string Message { get; set; }

        // JWT token for the newly registered user (optional)
        public string JwtToken { get; private set; }

        // Inner class representing the structure of the registration form
        public class RegistrationInput
        {
            [Required]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            public string Fullname { get; set; }
        }

        // OnGet method to handle the page load (empty as there's no specific action on load)
        public void OnGet()
        {
        }

        // OnPostAsync method triggered when the registration form is submitted
        public async Task<IActionResult> OnPostAsync()
        {
            // Validate the model state (check if all form inputs are valid)
            if (!ModelState.IsValid)
            {
                return Page(); // Return the page with validation error messages
            }

            // Check if the username already exists in the database
            if (_context.Users.Any(u => u.Username == Input.Username))
            {
                Message = "Username is already taken."; // Display message if username is taken
                return Page();
            }

            // Create a new User object and hash the password
            var user = new User
            {
                Username = Input.Username,
                Fullname = Input.Fullname,
                Password = HashPassword(Input.Password) // Store the hashed password
            };

            // Add the new user to the database and save changes
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate a JWT token for the newly registered user
            JwtToken = _jwtService.GenerateJwtToken(user.Username);

            // Optionally, store the JWT token in a secure HTTP-only cookie
            Response.Cookies.Append("JwtToken", JwtToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

            Message = "User registered successfully!"; // Display success message
            return RedirectToPage("/Identity/Login"); // Redirect to the login page after successful registration
        }

        // Method to hash the password with a salt using PBKDF2 (Password-Based Key Derivation Function 2)
        private string HashPassword(string password)
        {
            // Generate a random salt to add additional security to the password hash
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt); // Generate the salt
            }

            // Hash the password using PBKDF2 with the generated salt, HMACSHA256, and 10,000 iterations
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000, // Increase iterations for stronger security
                numBytesRequested: 256 / 8)); // 256 bits hash

            // Combine the salt and the hashed password, separated by a period
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }
    }
}
