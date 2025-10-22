using ECommerceSystem.GUI.Apis;
using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Refit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerceSystem.GUI.Services
{
    public class AuthService
    {
        private readonly IAuthApi _authApi;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        private const string TokenCookieName = "AuthToken";

        public AuthService(IAuthApi authApi, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _authApi = authApi;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Login
        public async Task<(bool success, string role, string token)> LoginAsync(LoginModel model)
        {
            try
            {
                // Gọi API login
                var response = await _authApi.Login(model); // API trả về { Token, User }

                if (string.IsNullOrEmpty(response?.Token))
                    return (false, null, null);

                var token = response.Token;

                SaveTokenToCookie(token); // Lưu vào cookie nếu muốn

                // Decode JWT lấy role
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
                var role = jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                // Tạo ClaimsPrincipal để MVC nhận diện user
                if (jwtToken != null)
                {
                    var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await _httpContextAccessor.HttpContext.SignInAsync(
                        "MyCookieAuth",
                        claimsPrincipal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                        });
                }

                return (true, role ?? "", token);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API login thất bại");
                return (false, null, null);
            }
        }

        // Register
        public async Task<bool> RegisterAsync(RegisterModel model)
        {
            try
            {
                var response = await _authApi.Register(model);
                return response != null;
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API register thất bại");
                return false;
            }
        }

        // Lấy user hiện tại từ JWT
        public UserInfo GetCurrentUser()
        {
            var token = GetTokenFromCookie();
            if (string.IsNullOrEmpty(token)) return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null) return null;

            return new UserInfo
            {
                Id = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                Email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                Name = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            };
        }

        // Logout
        public async Task LogoutAsync()
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            response?.Cookies.Delete(TokenCookieName);

            await _httpContextAccessor.HttpContext.SignOutAsync("MyCookieAuth");
        }

        // Helpers
        private void SaveTokenToCookie(string token)
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            response?.Cookies.Append(TokenCookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });
        }

        private string GetTokenFromCookie()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return request?.Cookies[TokenCookieName];
        }
    }
}
