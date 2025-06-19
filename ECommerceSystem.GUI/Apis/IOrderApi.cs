using ECommerceSystem.Shared.DTOs.Product;
using Refit;

namespace ECommerceSystem.GUI.Apis
{
    public interface IOrderApi
    {
        [Post("/api/user/orders/create")]
        Task<ApiResponse<OrderDTO>> CreateOrderAsync([Body] CreateOrderRequest request);
    }
}
