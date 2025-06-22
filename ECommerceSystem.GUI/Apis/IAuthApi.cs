using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.User;
using Refit;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Apis
{
    public interface IAuthApi
    {
        [Post("/api/auth/login")]
        Task<LoginResponse> Login([Body] LoginModel model);

        [Post("/api/auth/register")]    
        Task<RegisterResponse> Register([Body] RegisterModel model);

        [Get("/api/auth/role")]
        Task<RoleResponse> GetCurrentRole();

        [Post("/api/auth/refresh")]
        Task<RefreshTokenResponse> Refresh([Body] RefreshTokenRequest model);
    }
    public interface IUserApi
    {
        [Get("/api/admin/users")]
        Task<List<UserDTO>> GetAllAsync();

        [Get("/api/admin/users/{id}")]
        Task<UserDTO> GetByIdAsync(string id);

        [Put("/api/admin/users/{id}")]
        Task UpdateAsync(string id, [Body] UserDTO dto);

        [Delete("/api/admin/users/{id}")]
        Task SoftDeleteAsync(string id);
        [Post("/api/admin/users")]
        Task CreateAsync([Body] UserDTO dto);
        [Get("/api/admin/users/search")]
        Task<List<UserDTO>> SearchByNameAsync([Query] string name);

        [Post("/api/admin/users/delete-multiple")]
        Task SoftDeleteMultipleAsync([Body] List<string> ids);

    }

}
