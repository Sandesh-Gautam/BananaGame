using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BananaGame.Services
{
    // Service to handle JWT token generation and validation
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        // Constructor to inject IConfiguration (to access app settings like secret key, issuer, audience)
        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Method to generate a JWT token for the given username
        public string GenerateJwtToken(string username)
        {
            // Create claims for the JWT token, including the username and unique identifier
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username), // Username claim
                new Claim(JwtRegisteredClaimNames.Sub, username), // Subject claim (typically the username)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique ID (JWT ID)
            };

            // Get the secret key from the configuration and create a symmetric security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));

            // Define signing credentials using the symmetric security key and HMAC SHA-256 algorithm
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the JWT token with specified issuer, audience, claims, and expiration time (1 hour)
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"], // Issuer of the token (application or service)
                audience: _configuration["Jwt:Audience"], // Audience for the token (who it’s intended for)
                claims: claims, // Claims associated with the token
                expires: DateTime.Now.AddHours(1), // Set token expiration time to 1 hour
                signingCredentials: creds // Signing credentials to sign the token
            );

            // Convert the JWT token to a string and return it
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Method to validate the JWT token and return the ClaimsPrincipal (user identity)
        public ClaimsPrincipal ValidateToken(string token)
        {
            // Create a token handler to read and validate the token
            var tokenHandler = new JwtSecurityTokenHandler();

            // Get the secret key from the configuration to validate the token’s signature
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);

            // Define validation parameters for the token (issuer, audience, lifetime, and signing key)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, // Ensure the token's issuer is valid
                ValidateAudience = true, // Ensure the token's audience is valid
                ValidateLifetime = true, // Ensure the token is not expired
                ValidateIssuerSigningKey = true, // Ensure the signing key matches the one used to sign the token
                IssuerSigningKey = new SymmetricSecurityKey(key), // Key used to validate the token's signature
                ValidIssuer = _configuration["Jwt:Issuer"], // Expected issuer for the token
                ValidAudience = _configuration["Jwt:Audience"] // Expected audience for the token
            };

            try
            {
                // Try to validate the token using the specified parameters
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal; // Return the ClaimsPrincipal (user identity) if validation is successful
            }
            catch
            {
                // If token validation fails, return null (invalid token)
                return null;
            }
        }
    }
}
