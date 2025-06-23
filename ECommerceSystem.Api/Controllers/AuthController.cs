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
    [Authorize(Roles = "Admin")]
    [AllowAnonymous] // Cho phép truy cập công khai (GET danh mục)
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepo;
        private readonly IConfiguration _config;

        // Inject repository và cấu hình từ DI container
        public AuthController(UserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        // [POST] /api/auth/login
        // Đăng nhập và trả về JWT token nếu hợp lệ
        [HttpPost("login")]
        [AllowAnonymous] // Không yêu cầu đăng nhập trước
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Lấy user theo tên đăng nhập và kiểm tra mật khẩu
            var user = await _userRepo.GetUserPasswordHash(model.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Tên đăng nhập hoặc mật khẩu không đúng." });
            }

            // Lấy thông tin người dùng và vai trò
            var (userDto, role) = await _userRepo.GetUserInfoAndRole(user.UserName);

            // Tạo danh sách claim chứa thông tin người dùng để đưa vào token
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            // Tạo khóa ký token từ cấu hình
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tạo JWT token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            // Trả về token, vai trò và ID người dùng
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = role,
                userId = user.Id
            });
        }

        // [POST] /api/auth/register
        // Đăng ký tài khoản mới
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Kiểm tra tên đăng nhập đã tồn tại hay chưa
            var existing = await _userRepo.GetUserPasswordHash(model.UserName);
            if (existing != null)
            {
                return BadRequest(new { error = "Tên đăng nhập đã tồn tại." });
            }

            // Kiểm tra và tạo vai trò 'User' nếu chưa có
            var role = await _userRepo.GetRoleByName("User");
            if (role == null)
            {
                role = new Role { Name = "User" };
                await _userRepo.CreateRoleAsync(role); // Phải có method này trong repository
            }

            // Băm mật khẩu bằng BCrypt
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Tạo đối tượng User mới
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
                // Lưu user vào database
                await _userRepo.CreateUserAsync(user, hashedPassword);
                return Ok(new { message = "Đăng ký thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // [POST] /api/auth/role
        // Lấy vai trò hiện tại của người dùng từ JWT token
        [HttpPost("role")]
        [Authorize]
        public async Task<IActionResult> GetUserRole()
        {
            // Trích xuất ID từ token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Không tìm thấy người dùng trong token." });
            }

            // Tìm người dùng theo ID
            var user = await _userRepo.GetUserPasswordHashById(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { error = "Người dùng không tồn tại." });
            }

            // Trả về vai trò
            var (_, role) = await _userRepo.GetUserInfoAndRole(user.UserName);
            return Ok(new { role = role });
        }

        // [POST] /api/auth/refresh
        // Tạm thời trả về token mới cố định (cần làm thực tế sau)
        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] RefreshTokenRequest model)
        {
            // TODO: Thêm logic xử lý refresh token thực tế (lưu & kiểm tra trong DB)
            return Ok(new RefreshTokenResponse { AccessToken = "new-token", RefreshToken = "new-refresh" });
        }
    }
}
