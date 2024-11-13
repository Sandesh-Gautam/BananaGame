using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using BananaGame.Data;
using BananaGame.Models;

namespace BananaGame.Services
{
    public class RoleAuthorizationHandler : AuthorizationHandler<RolesRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoleAuthorizationHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesRequirement requirement)
        {
            // Get the logged-in user identity (user's ID)
            var userIdentity = _httpContextAccessor.HttpContext.User.Identity.Name;

            if (string.IsNullOrEmpty(userIdentity))
            {
                return Task.CompletedTask; // If no user is logged in, exit
            }

            // Safely parse the userId
            var userId = int.Parse(userIdentity);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return Task.CompletedTask; // If the user does not exist in the database, exit
            }

            // Check if the user has the role "UserRole.User" (role == 0)
            if (user.Role == UserRole.User)
            {
                context.Succeed(requirement); // Authorize the user if their role is 0 (User)
                return Task.CompletedTask;
            }

            // Convert requirement.Role (string) to UserRole enum for other roles
            if (Enum.TryParse(requirement.Role, out UserRole role))
            {
                if (user.Role != role)
                {
                    return Task.CompletedTask; // If the user's role doesn't match the required role, exit
                }
            }
            else
            {
                // Handle the case where the Role cannot be parsed into UserRole
                return Task.CompletedTask;
            }

            context.Succeed(requirement); // Succeed if the role matches

            return Task.CompletedTask;
        }
    }
}
