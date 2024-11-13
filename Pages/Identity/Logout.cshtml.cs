using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BananaGame.Pages.Identity
{
    public class LogoutModel : PageModel
    {
        private readonly IAuthenticationService _authenticationService;

        public LogoutModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Sign out the user
            await HttpContext.SignOutAsync();

            // Redirect to the home page or any other page after logout
            return RedirectToPage("/Index");
        }
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Clear the session data
            HttpContext.Session.Clear();  

          

            // Redirect to the login page or home page
            return RedirectToPage("/Account/Login");
        }

    }
}
