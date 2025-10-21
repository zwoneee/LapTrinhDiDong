using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.DTOs.Product;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceSystem.API.Controllers
{
    [Route("api/user/products")]
    [ApiController]
    [Authorize] // yêu cầu đăng nhập
    public class ProductRatingsController : ControllerBase
    {
        private readonly WebDBContext _dbContext;

        public ProductRatingsController(WebDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // POST /api/user/products/{id}/rate
        [HttpPost("{id}/rate")]
        public async Task<IActionResult> Rate(int id, [FromBody] RatingRequest request)
        {
            if (request == null || request.Value < 1 || request.Value > 5)
                return BadRequest(new { message = "Giá trị rating phải từ 1 đến 5" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var product = await _dbContext.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Sản phẩm không tồn tại" });

            // Nếu user đã rating trước đó -> update, ngược lại insert mới
            var existing = await _dbContext.ProductRatings
                .FirstOrDefaultAsync(r => r.ProductId == id && r.UserId == userId);

            if (existing != null)
            {
                existing.Value = request.Value;
                existing.CreatedAt = DateTime.UtcNow;
                _dbContext.ProductRatings.Update(existing);
            }
            else
            {
                var rating = new ProductRating
                {
                    ProductId = id,
                    UserId = userId,
                    Value = request.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.ProductRatings.Add(rating);
            }

            await _dbContext.SaveChangesAsync();

            // Tính lại trung bình và lưu vào Product.Rating
            var avg = await _dbContext.ProductRatings
                .Where(r => r.ProductId == id)
                .AverageAsync(r => (double)r.Value);

            product.Rating = (float)avg;
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();

            return Ok(new { average = avg });
        }

        // NEW: GET /api/user/products/{id}/rating
        // Trả về rating của user hiện tại cho product (hoặc null)
        [HttpGet("{id}/rating")]
        public async Task<IActionResult> GetUserRating(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var existing = await _dbContext.ProductRatings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ProductId == id && r.UserId == userId);

            // Trả về object { value = int? } (null nếu chưa rating)
            var value = existing != null ? (int?)existing.Value : null;
            return Ok(new { value });
        }
    }
}
