using Microsoft.AspNetCore.Authorization;

namespace BananaGame.Services
{
    public class RolesRequirement : IAuthorizationRequirement
    {
        public string Role { get; }

        public RolesRequirement(string role)
        {
            Role = role;
        }
    }
}
