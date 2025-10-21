using ECommerceSystem.GUI.Apis;
using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;

namespace ECommerceSystem.GUI.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartApi _cartApi;

        public CartController(ICartApi cartApi)
        {
            _cartApi = cartApi;
        }

        // Hiển thị giỏ hàng
        public async Task<IActionResult> Index()
        {
            try
            {
                var cart = await _cartApi.GetCart();
                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể tải giỏ hàng: " + ex.Message;
                return RedirectToAction("Error", "Home");
            }

        }

        // Thêm sản phẩm vào giỏ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var request = new AddToCartRequest
                {
                    ProductId = productId,
                    Quantity = quantity
                };

                await _cartApi.AddItem(request);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể thêm sản phẩm.";
                return RedirectToAction("Index");
            }
        }

        // Trả về tổng số lượng item trong giỏ (JSON) để client cập nhật badge
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            try
            {
                var cart = await _cartApi.GetCart();
                var count = cart?.Items?.Sum(i => i.Quantity) ?? 0;
                return Json(new { count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        // Xoá sản phẩm
        [HttpPost]
        public async Task<IActionResult> DeleteItem(int productId)
        {
            try
            {
                await _cartApi.RemoveItem(productId);
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể xoá sản phẩm.";
                return RedirectToAction("Index");
            }
        }

        // GET: Checkout
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cart = await _cartApi.GetCart();
                if (cart == null || cart.Items.Count == 0)
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống.";
                    return RedirectToAction("Index");
                }

                ViewBag.Cart = cart;
                return View(new CheckoutModel());
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể truy cập trang thanh toán.";
                return RedirectToAction("Index");
            }
        }

        // POST: Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutModel model)
        {
            if (!ModelState.IsValid)
            {
                var cart = await _cartApi.GetCart();
                ViewBag.Cart = cart;
                return View(model); // Giữ lại form để người dùng sửa
            }

            try
            {
                var response = await _cartApi.Checkout(model);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Thanh toán thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Thanh toán thất bại.";
                    return RedirectToAction("Checkout");
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi thanh toán.";
                return RedirectToAction("Checkout");
            }
        }
    }
}
