using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Shared.Constants;
using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.Shared.DTOs.Product;
using ECommerceSystem.Shared.Entities;
using ECommerceSystem.Shared.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using ECommerceSystem.Shared.DTOs.PayOrders;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/user/orders")]
    [ApiController]
    // [Authorize]
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

        [HttpPost("pay-all")]
        public async Task<IActionResult> PayAllOrders([FromBody] PayAllOrdersRequest request)
        {
            var pendingOrders = await _dbContext.Orders
                .Where(o => o.UserId == request.UserId && o.Status == OrderStatus.Pending)
                .Include(o => o.OrderItems)
                .ToListAsync();

            if (!pendingOrders.Any())
                return BadRequest("Không có đơn hàng đang chờ thanh toán.");

            var total = pendingOrders.Sum(o => o.Total);

            var receipt = new PaymentReceipt
            {
                UserId = request.UserId,
                TotalAmount = total,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = DateTime.UtcNow,
                Orders = pendingOrders
            };

            _dbContext.PaymentReceipts.Add(receipt);

            // Cập nhật trạng thái đơn hàng hoặc xóa hẳn:
            // Cách 1: đánh dấu đã thanh toán
            foreach (var order in pendingOrders)
            {
                order.Status = OrderStatus.Paid;
            }

            // Cách 2: xóa khỏi database
            // _dbContext.Orders.RemoveRange(pendingOrders);

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                Message = "Thanh toán thành công",
                ReceiptId = receipt.Id,
                Total = receipt.TotalAmount,
                OrderIds = pendingOrders.Select(o => o.Id)
            });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                UserId = o.UserId,
                Total = o.Total,
                Status = o.Status,
                DeliveryLocation = o.DeliveryLocation,
                QrCode = o.QrCode,
                Items = o.OrderItems.Select(i => new OrderItemDTO
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            }).ToList();

            return Ok(result);
        }
    }
}
