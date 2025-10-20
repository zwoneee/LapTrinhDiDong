﻿using ECommerceSystem.GUI.Services;
using ECommerceSystem.Shared.DTOs.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Refit;
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
                var (success, role) = await _authService.LoginAsync(model);

                if (!success || string.IsNullOrEmpty(role))
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                    ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    return View(model);
                }

                _logger.LogInformation("Successful login for username: {Username}", model.Username);

                // Điều hướng theo vai trò
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

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                // Log các lỗi nếu ModelState không hợp lệ
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        _logger.LogWarning("Lỗi trường {Field}: {Error}", entry.Key, error.ErrorMessage);
                    }
                }

                // Thông báo lỗi nếu ModelState không hợp lệ
                ViewBag.ErrorMessage = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            try
            {
                // Gọi dịch vụ đăng ký người dùng
                var success = await _authService.RegisterAsync(model);

                if (!success)
                {
                    // Nếu đăng ký thất bại, log cảnh báo và hiển thị thông báo lỗi
                    _logger.LogWarning("Đăng ký thất bại cho username: {Username}", model.UserName);
                    ViewBag.ErrorMessage = "Đăng ký thất bại. Vui lòng thử lại.";
                    return View(model);
                }

                // Đăng ký thành công, log thông tin
                _logger.LogInformation("Đăng ký thành công cho username: {Username}", model.UserName);

                // Tự động đăng nhập sau khi đăng ký
                var (loginSuccess, role) = await _authService.LoginAsync(new LoginModel
                {
                    Username = model.UserName,
                    Password = model.Password
                });

                if (loginSuccess)
                {
                    // Đăng nhập thành công, log thông tin và điều hướng về trang chủ
                    _logger.LogInformation("Tự động đăng nhập thành công cho username: {Username}", model.UserName);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Đăng nhập thất bại, log cảnh báo và yêu cầu người dùng đăng nhập thủ công
                    _logger.LogWarning("Tự động đăng nhập thất bại cho username: {Username}", model.UserName);
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
            }
            catch (ApiException apiEx)
            {
                // Xử lý lỗi từ API
                _logger.LogError($"Lỗi API khi đăng ký cho username: {model.UserName}. Lỗi: {apiEx.Message}");
                ViewBag.ErrorMessage = $"Lỗi khi tạo tài khoản từ API: {apiEx.Message}";
                return View(model);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi bất thường
                _logger.LogError($"Lỗi không xác định khi đăng ký cho username: {model.UserName}. Lỗi: {ex.Message}");
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.";
                return View(model);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("MyCookieAuth");
                _authService.Logout(); // Xóa cookie token
                _logger.LogInformation("User logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout process.");
                return View("Error");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
