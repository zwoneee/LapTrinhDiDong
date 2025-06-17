using ECommerceSystem.Shared.DTOs;
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

    
}
