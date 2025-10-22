using ECommerceSystem.GUI.Services;
using ECommerceSystem.Shared.DTOs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.GUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        // Hiển thị trang đăng nhập
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Xử lý đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Gọi AuthService, trả về tuple (success, role, token)
            var (success, role, token) = await _authService.LoginAsync(model);

            if (!success)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View(model);
            }

            // Lấy thông tin user từ JWT hoặc AuthService
            var currentUser = _authService.GetCurrentUser();
            if (currentUser != null)
            {
                HttpContext.Session.SetString("UserId", currentUser.Id);
                HttpContext.Session.SetString("UserName", currentUser.Name ?? "");
                HttpContext.Session.SetString("UserRole", currentUser.Role ?? "");
            }

            // Trả về cùng Login.cshtml với token + user info
            ViewBag.Token = token;
            ViewBag.UserId = currentUser?.Id;
            ViewBag.UserName = currentUser?.Name;
            ViewBag.Role = role;

            return View("Login"); // Không tạo view mới
        }

        // Đăng xuất
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();

            Response.Cookies.Delete("AuthToken");
           

            return RedirectToAction("Login", "Account");
        }
    }
}
