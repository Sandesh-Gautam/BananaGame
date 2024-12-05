using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Linq;
using BananaGame.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BananaGame.Services;

namespace BananaGame.Pages.Identity
{
    // PageModel for the login page
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        // Constructor injects the database context and JWT service
        public LoginModel(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // Property to bind the login form inputs
        [BindProperty]
        public LoginInput Input { get; set; }

        // Message to show feedback to the user
        public string Message { get; set; }

        // Property to store the JWT token for successful login
        public string JwtToken { get; private set; }

        // Model to capture login input (username and password)
        public class LoginInput
        {
            [Required] // Ensures the username is provided
            public string Username { get; set; }

            [Required] // Ensures the password is provided
            [DataType(DataType.Password)] // Ensures password is treated securely
            public string Password { get; set; }
        }

        // On GET request: Render the login page
        public void OnGet()
        {
        }

        // On POST request: Handle user login
        public async Task<IActionResult> OnPost()
        {
            // Check if the model state is valid (all required fields are filled)
            if (!ModelState.IsValid)
            {
                return Page(); // Return to the same page if validation fails
            }

            // Attempt to retrieve the user from the database by username
            var user = _context.Users.SingleOrDefault(u => u.Username == Input.Username);
            if (user == null || !VerifyPassword(Input.Password, user.Password))
            {
                // Invalid username or password
                Message = "Invalid username or password.";
                return Page(); // Return to the login page with an error message
            }

            // Generate a JWT token for the user upon successful login
            JwtToken = _jwtService.GenerateJwtToken(user.Username);

            // Create claims to store user information in the cookie authentication system
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Store the user's ID
                new Claim(ClaimTypes.Name, user.Username), // Store the user's username
                new Claim("FullName", user.Fullname), // Store the user's full name
                new Claim("LoginTime", DateTime.UtcNow.ToString()) // Track login time
            };

            // Create an identity from the claims and specify the authentication scheme
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user with cookies (this creates a session)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Optionally store the JWT token in a secure HttpOnly cookie for API use
            Response.Cookies.Append("JwtToken", JwtToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

            // Set a success message
            Message = "Login successful!";

            // Redirect the user to the dashboard page after successful login
            return RedirectToPage("/Game/Dashboard");
        }

        // Helper method to verify the password by hashing it and comparing it to the stored hash
        private bool VerifyPassword(string providedPassword, string storedPassword)
        {
            // Split the stored password into salt and hash parts
            var parts = storedPassword.Split('.');
            if (parts.Length != 2)
                return false; // Return false if the stored password format is invalid

            // Decode the stored salt and hash
            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];

            // Hash the provided password using the stored salt and the same hashing algorithm
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: providedPassword, // Provided password from input
                salt: salt, // Stored salt
                prf: KeyDerivationPrf.HMACSHA256, // Hashing algorithm
                iterationCount: 10000, // Number of iterations
                numBytesRequested: 256 / 8)); // Desired hash length

            // Compare the computed hash with the stored hash
            return hash == storedHash;
        }
    }
}
