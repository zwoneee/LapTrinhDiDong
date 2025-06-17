using ECommerceSystem.GUI.Services;
using ECommerceSystem.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        _logger.LogWarning("Lỗi trường {Field}: {Error}", entry.Key, error.ErrorMessage);
                    }
                }

                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            try
            {
                var success = await _authService.LoginAsync(model);

                if (!success)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                    ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return View(model);
                }

                _logger.LogInformation("Successful login for username: {Username}", model.Username);
                var role = await _authService.GetCurrentRoleAsync();

                return role switch
                {
                    "Admin" => RedirectToAction("Index", "Admin"),
                    "Customer" => RedirectToAction("Index", "Home"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for username: {Username}", model.Username);
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            _authService.Logout();
            _logger.LogInformation("User logged out successfully.");
            return RedirectToAction("Index", "Home");
        }
    }
}
