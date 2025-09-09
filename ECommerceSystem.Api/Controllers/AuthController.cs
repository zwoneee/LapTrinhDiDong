using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.User;

namespace ECommerceSystem.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepo;
        private readonly IConfiguration _config;

        public AuthController(UserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userRepo.GetUserPasswordHash(model.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized(new { error = "Tên đăng nhập hoặc mật khẩu không đúng." });

            var (userDto, role) = await _userRepo.GetUserInfoAndRole(user.UserName);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role,
                userId = user.Id
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _userRepo.GetUserPasswordHash(model.UserName);
            if (existing != null)
                return BadRequest(new { error = "Tên đăng nhập đã tồn tại." });

            // Always get or create the "User" role and ensure its Id is used
            var role = await _userRepo.GetRoleByName("User");
            if (role == null)
            {
                role = new Role { Name = "User" };
                await _userRepo.CreateRoleAsync(role);
                // Fetch again to get the generated Id
                role = await _userRepo.GetRoleByName("User");
            }

            var user = new User
            {
                UserName = model.UserName,
                Name = model.Name,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeviceToken = "" // hoặc Guid.NewGuid().ToString()
            };

            try
            {
                await _userRepo.CreateUserAsync(user, user.PasswordHash);
                return Ok(new { message = "Đăng ký thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Lỗi khi đăng ký: {ex.Message}" });
            }
        }

        [HttpPost("role")]
        [Authorize]
        public async Task<IActionResult> GetUserRole()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { error = "Token không hợp lệ." });

                var user = await _userRepo.GetUserPasswordHashById(int.Parse(userIdClaim));
                if (user == null)
                    return NotFound(new { error = "Người dùng không tồn tại." });

                var (_, role) = await _userRepo.GetUserInfoAndRole(user.UserName);
                return Ok(new { role });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Lỗi lấy vai trò: {ex.Message}" });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] RefreshTokenRequest model)
        {
            // TODO: Xử lý logic refresh token thực tế ở đây
            return Ok(new RefreshTokenResponse
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token"
            });
        }
    }
}
