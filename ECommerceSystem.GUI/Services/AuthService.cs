using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.GUI.Apis;
using Microsoft.AspNetCore.Http;
using Refit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Services
{
    public class AuthService
    {
        private readonly IAuthApi _authApi;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string TokenCookieName = "AuthToken";

        public AuthService(IAuthApi authApi, IHttpContextAccessor httpContextAccessor)
        {
            _authApi = authApi;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<bool> LoginAsync(LoginModel model)
        {
            try
            {
                var response = await _authApi.Login(model);

                // Ghi log phản hồi từ API
                Console.WriteLine($"Login API Response: {JsonSerializer.Serialize(response)}");

                if (!string.IsNullOrWhiteSpace(response.Token))
                {
                    SaveTokenToCookie(response.Token);
                    return true;
                }

                // Ghi log nếu token null hoặc rỗng
                Console.WriteLine("Login failed: Token is null or empty");
                return false;
            }
            catch (ApiException ex)
            {
                // Ghi log lỗi API
                Console.WriteLine($"Login API Error: {ex.StatusCode} - {ex.Message}");
                return false;
            }
        }


        public async Task<bool> RegisterAsync(RegisterModel model)
        {
            try
            {
                var response = await _authApi.Register(model);
                return response != null;
            }
            catch (ApiException ex)
            {
                return false;
            }
        }

        public async Task<string> GetCurrentRoleAsync()
        {
            var token = GetTokenFromCookie();
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var response = await _authApi.GetCurrentRole();
                return response.Role;
            }
            catch
            {
                return null;
            }
        }

        public void Logout()
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            response?.Cookies.Delete(TokenCookieName);
        }

        public void SaveTokenToCookie(string token)
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

        public string GetTokenFromCookie()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return request?.Cookies[TokenCookieName];
        }

        public UserInfo GetCurrentUser()
        {
            var token = GetTokenFromCookie();
            if (string.IsNullOrEmpty(token)) return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null) return null;

            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            return new UserInfo
            {
                Id = userId,
                Email = email,
                Role = role,
                Name = name
            };
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var refreshToken = GetRefreshTokenFromCookie();
            if (string.IsNullOrEmpty(refreshToken)) return false;

            try
            {
                var response = await _authApi.Refresh(new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                });

                SaveTokenToCookie(response.AccessToken);
                SaveRefreshTokenToCookie(response.RefreshToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Add this method to fix the CS0103 error
        public void SaveRefreshTokenToCookie(string refreshToken)
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            response?.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7) // Adjust expiration as needed
            });
        }

        // Add this helper method to retrieve the refresh token from the cookie
        public string GetRefreshTokenFromCookie()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            return request?.Cookies["RefreshToken"];
        }

    }
}
