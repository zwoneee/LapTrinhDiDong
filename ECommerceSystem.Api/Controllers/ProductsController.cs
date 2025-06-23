using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.Shared.Entities;
using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/public/products")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [AllowAnonymous] // Cho phép truy cập công khai (GET danh mục)
    public class ProductsController : ControllerBase
    {
        private readonly WebDBContext _dbContext;
        private readonly MongoDbContext _mongoContext;
        private readonly IDistributedCache _cache;

        // Inject SQL DbContext, MongoDbContext và cache Redis
        public ProductsController(WebDBContext dbContext, MongoDbContext mongoContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _mongoContext = mongoContext;
            _cache = cache;
        }

        /// <summary>
        /// [GET] /api/public/products
        /// Truy vấn danh sách sản phẩm theo bộ lọc, phân trang, cache và ghi log tìm kiếm vào MongoDB
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            bool? promotion,
            int page = 1,
            int pageSize = 10)
        {
            // Tạo khóa cache dựa trên các tham số truy vấn
            var cacheKey = $"products_{search}_{categoryId}_{minPrice}_{maxPrice}_{sortBy}_{promotion}_{page}_{pageSize}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);

            // Nếu có cache thì trả về luôn
            if (!string.IsNullOrEmpty(cachedResult))
                return Ok(JsonSerializer.Deserialize<object>(cachedResult));

            // Query cơ bản: sản phẩm chưa bị xóa
            var query = _dbContext.Products.Where(p => !p.IsDeleted);

            // Bộ lọc nâng cao
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice);

            if (promotion.HasValue)
                query = query.Where(p => p.IsPromoted == promotion);

            // Sắp xếp
            query = sortBy switch
            {
                "priceAsc" => query.OrderBy(p => p.Price),
                "priceDesc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Rating),
                _ => query.OrderBy(p => p.Id)
            };

            // Lấy tổng số sản phẩm
            var total = await query.CountAsync();

            // Phân trang kết quả và chuyển về DTO
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    ThumbnailUrl = p.ThumbnailUrl,
                    CategoryId = p.CategoryId,
                    Rating = p.Rating,
                    IsPromoted = p.IsPromoted,
                    QrCode = p.QrCode
                })
                .ToListAsync();

            // Lưu từ khóa tìm kiếm vào MongoDB để phân tích hành vi người dùng
            if (!string.IsNullOrEmpty(search))
            {
                await _mongoContext.Preferences.InsertOneAsync(new UserPreference
                {
                    GuestId = Guid.NewGuid().ToString(),
                    SearchHistory = new List<string> { search },
                    Timestamp = DateTime.UtcNow
                });
            }

            // Cache kết quả
            var result = new { total, page, pageSize, products };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(result);
        }

        /// <summary>
        /// [POST] /api/public/products
        /// Thêm sản phẩm mới (chỉ Admin được phép)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody] ProductDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Tạo entity từ DTO
            var product = new Product
            {
                Name = model.Name,
                Price = model.Price,
                Description = model.Description,
                ThumbnailUrl = model.ThumbnailUrl,
                CategoryId = model.CategoryId,
                Stock = model.Stock,
                Rating = model.Rating,
                IsPromoted = model.IsPromoted,
                QrCode = model.QrCode,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                Slug = model.Slug
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }

        /// <summary>
        /// [PUT] /api/public/products/{id}
        /// Sửa thông tin sản phẩm (chỉ Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(int id, [FromBody] ProductDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _dbContext.Products.FindAsync(id);
            if (product == null || product.IsDeleted)
                return NotFound();

            // Cập nhật thông tin sản phẩm
            product.Name = model.Name;
            product.Price = model.Price;
            product.Description = model.Description;
            product.ThumbnailUrl = model.ThumbnailUrl;
            product.CategoryId = model.CategoryId;
            product.Stock = model.Stock;
            product.Rating = model.Rating;
            product.IsPromoted = model.IsPromoted;
            product.QrCode = model.QrCode;
            product.UpdatedAt = DateTime.UtcNow;
            product.Slug = model.Slug;

            await _dbContext.SaveChangesAsync();

            return Ok(product);
        }

        /// <summary>
        /// [DELETE] /api/public/products/{id}
        /// Xóa sản phẩm (mềm) – chỉ Admin
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null || product.IsDeleted)
                return NotFound();

            // Xóa mềm – nếu muốn xóa cứng thì dùng Remove()
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}
