using BananaGame.Data;
using BananaGame.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();


// Register ApplicationDbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session services with a 1-day timeout
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register IHttpContextAccessor as Singleton 
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register RoleAuthorizationHandler as Scoped, because it depends on ApplicationDbContext (which is Scoped)
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();


// Add Authorization services
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserRolePolicy", policy => policy.Requirements.Add(new RolesRequirement("User")));
});

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Identity/Login";
                options.LogoutPath = "/Identity/Logout";
            });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication(); // Make sure authentication is used
app.UseAuthorization(); // Ensure authorization is applied
app.UseSession(); // Enable session state for the application

app.MapRazorPages(); // Map Razor Pages


app.Run();
