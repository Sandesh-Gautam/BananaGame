using BananaGame.Services;

// Import necessary namespaces for authentication, authorization, and session management
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages(); // Add Razor Pages for UI handling
builder.Services.AddHttpClient(); // Register HttpClient for making HTTP requests

// Register ApplicationDbContext with SQL Server connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// This sets up the application's connection to the database using the connection string named "DefaultConnection"

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Configure JWT Bearer authentication
    .AddJwtBearer(options =>
    {
        // Define parameters for token validation, including issuer, audience, and signing key
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Ensure that the token's issuer is valid
            ValidateAudience = true, // Ensure that the token's audience is valid
            ValidateLifetime = true, // Ensure the token has not expired
            ValidateIssuerSigningKey = true, // Ensure the signing key used to generate the token is valid
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // Set the valid issuer for JWT
            ValidAudience = builder.Configuration["Jwt:Audience"], // Set the valid audience for JWT
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])) // Set the secret key used for signing the JWT
        };
    });

// Add Session services with a 1-day timeout for session persistence
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1); // Set session timeout to 1 day
    options.Cookie.HttpOnly = true; // Prevent access to session cookie from JavaScript
    options.Cookie.IsEssential = true; // Mark session cookie as essential
});

// Register IHttpContextAccessor to access HTTP context, needed for retrieving session data
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
// Register JwtService as a Singleton, so it is available throughout the application
builder.Services.AddSingleton<JwtService>();

// Add Cookie Authentication with paths for login and logout redirection
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Identity/Login"; // Redirect to Login page if unauthenticated
        options.LogoutPath = "/Identity/Logout"; // Redirect to Logout page after logging out
    });

var app = builder.Build();

// Configure the HTTP request pipeline

// If not in development environment, use error handling and HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // Use custom error handling page
    app.UseHsts(); // Add HTTP Strict Transport Security headers
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
app.UseStaticFiles(); // Serve static files like images, CSS, JS

app.UseRouting(); // Enable routing for handling requests
app.UseAuthentication(); // Enable authentication middleware to check for valid tokens or cookies
app.UseAuthorization(); // Enable authorization middleware to ensure users have appropriate permissions
app.UseSession(); // Enable session middleware to manage user sessions

// Set the fallback route in case of missing paths
app.MapFallbackToPage("/Game/Dashboard"); // Redirect unknown paths to the dashboard page

// Map Razor Pages to handle HTTP requests to the page routes
app.MapRazorPages(); // Route Razor Pages to handle requests to pages like login, registration, etc.

app.Run(); // Run the application
