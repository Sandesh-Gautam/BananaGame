using BananaGame.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace BananaGame.Pages.Game
{
    public class LeaderboardModel : PageModel
    {
      
        public List<UserHighscore> HighScores { get; set; } = new List<UserHighscore>();

        public void OnGet()
        {
          
        }

     

   

        
    }
}
