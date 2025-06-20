using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.DTOs.User;
using ECommerceSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Data.Repositories
{
    public class UserRepository
    {
        private readonly WebDBContext _dbContext;

        public UserRepository(WebDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserDTO> GetUserInfo(string username)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);
            if (user == null) return null;

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
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);

            if (user == null) return (null, null);

            return (new UserDTO
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                Email = user.Email,
                DeviceToken = user.DeviceToken,
                IsDeleted = user.IsDeleted
            }, user.Role?.Name);
        }

        public async Task<User> GetUserPasswordHash(string username)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted);
        }

        public async Task<Role> GetRoleByName(string roleName)
        {
            return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        }

        public async Task CreateRoleAsync(Role role)
        {
            await _dbContext.Roles.AddAsync(role);
            await _dbContext.SaveChangesAsync();
        }

        public async Task CreateUserAsync(User user, string passwordHash)
        {
            user.PasswordHash = passwordHash; // đã mã hóa ở nơi khác, ví dụ bằng BCrypt.Net
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddUserToRoleAsync(User user, string roleName)
        {
            var role = await GetRoleByName(roleName);
            if (role == null)
            {
                throw new Exception($"Vai trò '{roleName}' không tồn tại.");
            }

            user.RoleId = role.Id;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<User> GetUserPasswordHashById(int id)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }
    }
}
