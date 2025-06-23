using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceSystem.Api.Repositories;
using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.Product;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Controllers
{
    [ApiController]
    [Route("api/user/cart")]
    //[Authorize] // Bật nếu bạn xác thực bằng JWT
    public class CartApiController : ControllerBase
    {
        private readonly ICartRepository _cartRepo;

        public CartApiController(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }

        /// <summary>
        /// Lấy giỏ hàng của người dùng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<CartDTO>> GetCart()
        {
            var cart = await _cartRepo.GetUserCart();
            return Ok(cart);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
        {
            var count = await _cartRepo.AddItem(request.ProductId, request.Quantity);
            return Ok(new { cartItemCount = count });
        }

        /// <summary>
        /// Xoá sản phẩm khỏi giỏ
        /// </summary>
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var count = await _cartRepo.RemoveItem(productId);
            return Ok(new { cartItemCount = count });
        }

        /// <summary>
        /// Lấy tổng số mặt hàng trong giỏ
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetCartItemCount()
        {
            var count = await _cartRepo.GetCartItemCount();
            return Ok(new { cartItemCount = count });
        }

        /// <summary>
        /// Thanh toán giỏ hàng
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cartRepo.DoCheckout(model);
            if (!result)
                return BadRequest(new { success = false, message = "Checkout thất bại" });

            return Ok(new { success = true, message = "Checkout thành công" });
        }
    }
}
