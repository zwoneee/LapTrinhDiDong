using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.User;

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;

        public AuthController(UserRepository userRepository, UserManager<User> userManager, IConfiguration config)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _config = config;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || user.IsDeleted)
            {
                return Unauthorized(new { error = "Tên đăng nhập hoặc mật khẩu không đúng." });
            }

            if (await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(role))
                {
                    return Unauthorized(new { error = "Người dùng không có vai trò được cấp quyền." });
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
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
                    role = role,
                    userId = user.Id
                });
            }

            return Unauthorized(new { error = "Tên đăng nhập hoặc mật khẩu không đúng." });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Email đã được sử dụng." });
            }

            // Tìm role 'User'
            var role = await _userRepository.GetRoleByName("User");
            if (role == null)
            {
                return StatusCode(500, new { error = "Role mặc định không tồn tại. Vui lòng tạo role 'User' trong DB." });
            }

            // Khởi tạo user
            var user = new User
            {
                UserName = model.UserName,
                Name = model.Name,
                Email = model.Email,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeviceToken = ""
            };

            try
            {
                // Tạo user
                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { error = "Không thể tạo người dùng.", details = result.Errors });
                }

                // Gán vào vai trò 'User'
                await _userManager.AddToRoleAsync(user, "User");

                return Ok(new { message = "Đăng ký thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
        {
            // Validate refreshToken → cấp lại accessToken mới
            return Ok(new RefreshTokenResponse { AccessToken = "new...", RefreshToken = "new..." });
        }


        [HttpPost("role")]
        //[Authorize]
        public async Task<IActionResult> GetUserRole()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Không tìm thấy userId trong token");
                return Unauthorized(new { message = "Không tìm thấy ID người dùng trong mã thông báo" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"Không tìm thấy người dùng với ID: {userId}");
                return NotFound(new { message = "Không tìm thấy người dùng" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || !roles.Any())
            {
                Console.WriteLine($"Không tìm thấy vai trò cho người dùng ID: {userId}");
                return NotFound(new { message = "Không có vai trò nào được gán cho người dùng" });
            }

            Console.WriteLine($"Vai trò tìm thấy: {roles.First()}");
            return Ok(new { role = roles.First() });
        }
    }

    
}