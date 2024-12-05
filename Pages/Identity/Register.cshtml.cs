using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BananaGame.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using BananaGame.Models;
using BananaGame.Services;
using System.Linq;

namespace BananaGame.Pages.Identity
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public RegisterModel(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [BindProperty]
        public RegistrationInput Input { get; set; }

        public string Message { get; set; }
        public string JwtToken { get; private set; }

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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if username is already taken
            if (_context.Users.Any(u => u.Username == Input.Username))
            {
                Message = "Username is already taken.";
                return Page();
            }

            // Create a new user and hash the password
            var user = new User
            {
                Username = Input.Username,
                Fullname = Input.Fullname,
                Password = HashPassword(Input.Password)
            };

            // Save the user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT Token for the new user
            JwtToken = _jwtService.GenerateJwtToken(user.Username);

            // Optionally, store JWT in a cookie or return it to the client
            Response.Cookies.Append("JwtToken", JwtToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

            Message = "User registered successfully!";
            return RedirectToPage("/Identity/Login");
        }

        private string HashPassword(string password)
        {
            // Generate a salt
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Combine salt and hashed password
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }
    }
}
