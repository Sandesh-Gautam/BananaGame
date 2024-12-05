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
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public LoginModel(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [BindProperty]
        public LoginInput Input { get; set; }

        public string Message { get; set; }
        public string JwtToken { get; private set; }

        public class LoginInput
        {
            [Required]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Retrieve the user from the database
            var user = _context.Users.SingleOrDefault(u => u.Username == Input.Username);
            if (user == null || !VerifyPassword(Input.Password, user.Password))
            {
                Message = "Invalid username or password.";
                return Page();
            }

            // Generate JWT Token
            JwtToken = _jwtService.GenerateJwtToken(user.Username);

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.Fullname),
            
                new Claim("LoginTime", DateTime.UtcNow.ToString()) // Track login time
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user with cookies
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Optionally, store JWT in a cookie or return it in the response for API use
            // Example: Cookie for JWT
            Response.Cookies.Append("JwtToken", JwtToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

            Message = "Login successful!";
            return RedirectToPage("/Game/Dashboard");
        }

        // Verify the password by extracting the salt and hashing the provided password
        private bool VerifyPassword(string providedPassword, string storedPassword)
        {
            // Split the stored password into salt and hash
            var parts = storedPassword.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = parts[1];

            // Hash the provided password using the stored salt
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Compare the hash with the stored hash
            return hash == storedHash;
        }
    }
}
