using ECommerceSystem.Shared.DTOs.Models;
using ECommerceSystem.Shared.DTOs.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Repositories
{
    public interface ICartRepository
    {
        /// <summary>
        /// Thêm một sản phẩm vào giỏ hàng của người dùng hiện tại.
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <param name="qty">Số lượng (mặc định là 1)</param>
        /// <returns>Số lượng sản phẩm trong giỏ sau khi thêm</returns>
        Task<int> AddItem(int productId, int qty = 1);

        /// <summary>
        /// Giảm hoặc xóa sản phẩm khỏi giỏ hàng của người dùng hiện tại.
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <returns>Số lượng sản phẩm còn lại trong giỏ</returns>
        Task<int> RemoveItem(int productId);

        /// <summary>
        /// Lấy giỏ hàng của người dùng hiện tại (dựa vào context).
        /// </summary>
        /// <returns>Thông tin giỏ hàng</returns>
        Task<CartDTO> GetUserCart();

        /// <summary>
        /// Lấy giỏ hàng theo ID người dùng cụ thể.
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <returns>Thông tin giỏ hàng</returns>
        Task<CartDTO> GetCart(string userId);

        /// <summary>
        /// Lấy tổng số sản phẩm trong giỏ hàng.
        /// </summary>
        /// <param name="userId">ID người dùng, nếu không truyền thì lấy từ context</param>
        /// <returns>Tổng số lượng</returns>
        Task<int> GetCartItemCount(string userId = "");

        /// <summary>
        /// Tiến hành thanh toán (tạo đơn hàng, xoá giỏ hàng).
        /// </summary>
        /// <param name="model">Thông tin thanh toán</param>
        /// <returns>Kết quả thanh toán</returns>
        Task<bool> DoCheckout(CheckoutModel model);
    }
}
