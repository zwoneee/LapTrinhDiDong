using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.DTOs.User;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Data.Repositories
{
    public class UserRepository
    {
        private readonly WebDBContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserRepository(WebDBContext dbContext, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<UserDTO> GetUserInfo(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.IsDeleted) return null;

            return new UserDTO
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                Email = user.Email,
                DeviceToken = user.DeviceToken,
                IsDeleted = user.IsDeleted
            };
        }

        public async Task<(UserDTO User, string Role)> GetUserInfoAndRole(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || user.IsDeleted) return (null, null);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            return (new UserDTO
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                Email = user.Email,
                DeviceToken = user.DeviceToken,
                IsDeleted = user.IsDeleted
            }, role);
        }

        public async Task<User> GetUserPasswordHash(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return user != null && !user.IsDeleted ? user : null;
        }

        public async Task<Role> GetRoleByName(string roleName)
        {
            return await _roleManager.FindByNameAsync(roleName);
        }

        public async Task CreateUserAsync(User user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task AddUserToRoleAsync(User user, string roleName)
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}