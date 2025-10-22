using ECommerceSystem.Shared.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Data.Repositories
{
    public interface IUserService
    {
        Task<List<UserDTO>> GetAllAsync();
        Task<UserDTO> GetByIdAsync(string id);
        Task UpdateAsync(string id, UserDTO dto);
        Task SoftDeleteAsync(string id);
        Task CreateAsync(UserDTO dto);
        Task<List<UserDTO>> SearchByNameAsync(string name);
        Task SoftDeleteMultipleAsync(List<string> ids);
    }
}
