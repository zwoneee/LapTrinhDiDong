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
    [Route("api/auth")]
    [ApiController]
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
            var user = await _userRepo.GetUserPasswordHash(model.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Tên đăng nhập hoặc mật khẩu không đúng." });
            }

            var (userDto, role) = await _userRepo.GetUserInfoAndRole(user.UserName);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            // ✅ Tạo khóa bí mật
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // ✅ Tạo JWT token ở đây
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
                role = role,
                userId = user.Id
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var existing = await _userRepo.GetUserPasswordHash(model.UserName);
            if (existing != null)
            {
                return BadRequest(new { error = "Tên đăng nhập đã tồn tại." });
            }

            // Kiểm tra và tạo Role 'User' nếu chưa có
            var role = await _userRepo.GetRoleByName("User");
            if (role == null)
            {
                role = new Role { Name = "User" };
                await _userRepo.CreateRoleAsync(role); // ⚠️ cần thêm method này trong UserRepository
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                UserName = model.UserName,
                Name = model.Name,
                Email = model.Email,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                PasswordHash = hashedPassword,
                DeviceToken = ""
            };

            try
            {
                await _userRepo.CreateUserAsync(user, hashedPassword);
                return Ok(new { message = "Đăng ký thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost("role")]
        [Authorize]
        public async Task<IActionResult> GetUserRole()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Không tìm thấy người dùng trong token." });
            }

            var user = await _userRepo.GetUserPasswordHashById(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { error = "Người dùng không tồn tại." });
            }

            var (_, role) = await _userRepo.GetUserInfoAndRole(user.UserName);
            return Ok(new { role = role });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] RefreshTokenRequest model)
        {
            // TODO: Xử lý token refresh thực tế
            return Ok(new RefreshTokenResponse { AccessToken = "new-token", RefreshToken = "new-refresh" });
        }
    }
}
