using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.Constants;
using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.Shared.Entities;
using ECommerceSystem.Shared.Utilities;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.AspNetCore.SignalR;
using System;

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/user/orders")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly WebDBContext _dbContext;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OrdersController(WebDBContext dbContext, IHubContext<NotificationHub> hubContext)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            foreach (var item in request.Items)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                    return BadRequest("Sản phẩm hết hàng hoặc không tồn tại");
            }

            var order = new Order
            {
                UserId = request.UserId,
                Total = request.Total,
                Status = OrderStatus.Pending,
                DeliveryLocation = request.DeliveryLocation,
                CreatedAt = DateTime.UtcNow,
                OrderItems = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            //order.QrCode = QrCodeGenerator.GenerateQrCode($"order_{order.Id}");
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var paymentUrl = "https://paypal.com/pay/abc123"; // Tích hợp SDK PayPal/Stripe sau

            await _hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", order.Id, order.Status);

            return CreatedAtAction(nameof(CreateOrder), new { orderId = order.Id }, new OrderDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                Total = order.Total,
                Status = order.Status,
                DeliveryLocation = order.DeliveryLocation,
                //QrCode = order.QrCode,
                Items = order.OrderItems.Select(i => new OrderItemDTO
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            });
        }
    }
}
