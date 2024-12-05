using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace BananaGame.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }


    }
}
