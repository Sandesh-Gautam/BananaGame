using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BananaGame.Pages.Identity
{
    // PageModel for handling user logout functionality
    public class LogoutModel : PageModel
    {
        private readonly IAuthenticationService _authenticationService;

        // Constructor injecting the IAuthenticationService to handle authentication actions
        public LogoutModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        // OnGetAsync is triggered when the user accesses the logout page
        public async Task<IActionResult> OnGetAsync()
        {
            // Sign out the user by removing the authentication cookies and session data
            await HttpContext.SignOutAsync();

            // After signing out, redirect the user to the Dashboard page
            // This could be adjusted to redirect to a login page if needed
            return RedirectToPage("/Game/Dashboard");
        }
    }
}
