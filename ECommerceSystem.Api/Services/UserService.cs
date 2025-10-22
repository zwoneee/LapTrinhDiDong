using ECommerceSystem.Shared.DTOs.User;
using ECommerceSystem.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Data.Repositories
{
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepo;

        public UserService(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task CreateAsync(UserDTO dto)
        {
            var user = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                DeviceToken = dto.DeviceToken,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            // TODO: thay bằng hash mật khẩu thật sự
            await _userRepo.CreateUserAsync(user, "hashed_password");
        }

        public Task<List<UserDTO>> GetAllAsync() => _userRepo.GetAllAsync();
        public Task<UserDTO> GetByIdAsync(string id) => _userRepo.GetByIdAsync(id);
        public Task UpdateAsync(string id, UserDTO dto) => _userRepo.UpdateAsync(id, dto);
        public Task SoftDeleteAsync(string id) => _userRepo.SoftDeleteAsync(id);
        public Task<List<UserDTO>> SearchByNameAsync(string name) => _userRepo.SearchByNameAsync(name);
        public Task SoftDeleteMultipleAsync(List<string> ids) => _userRepo.SoftDeleteMultipleAsync(ids);
    }
}
