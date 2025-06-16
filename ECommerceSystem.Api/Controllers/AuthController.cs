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

            // Tìm role 'Customer'
            var role = await _userRepository.GetRoleByName("Customer");
            if (role == null)
            {
                return StatusCode(500, new { error = "Role mặc định không tồn tại. Vui lòng tạo role 'Customer' trong DB." });
            }

            // Khởi tạo user
            var user = new User
            {
                UserName = model.Email,
                Name = model.Name,
                Email = model.Email,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                 DeviceToken = "" // ✅ Fix lỗi NULL ở đây
            };

            try
            {
                // Tạo user
                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { error = "Không thể tạo người dùng.", details = result.Errors });
                }

                // Gán vào vai trò 'Customer'
                await _userManager.AddToRoleAsync(user, "Customer");

                return Ok(new { message = "Đăng ký thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("role")]
        [Authorize]
        public IActionResult GetCurrentRole()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
            {
                return BadRequest(new { error = "Không tìm thấy vai trò của người dùng." });
            }
            return Ok(new { Role = role });
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}