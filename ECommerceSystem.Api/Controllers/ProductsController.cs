using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.Shared.DTOs.Product;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/public/products")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Mặc định chỉ Admin được POST, PUT, DELETE
    public class ProductsController : ControllerBase
    {
        private readonly WebDBContext _dbContext;
        private readonly IDistributedCache _cache;

        public ProductsController(WebDBContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        /// <summary>
        /// [GET] /api/public/products
        /// Lấy danh sách sản phẩm (công khai)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
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
            var cacheKey = $"products_{search}_{categoryId}_{minPrice}_{maxPrice}_{sortBy}_{promotion}_{page}_{pageSize}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
                return Ok(JsonSerializer.Deserialize<object>(cachedResult));

            var query = _dbContext.Products.Where(p => !p.IsDeleted);

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

            query = sortBy switch
            {
                "priceAsc" => query.OrderBy(p => p.Price),
                "priceDesc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Rating),
                _ => query.OrderBy(p => p.Id)
            };

            var total = await query.CountAsync();

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
                    QrCode = p.QrCode,
                    Stock = p.Stock,
                    Slug = p.Slug
                })
                .ToListAsync();

            var result = new { total, page, pageSize, products };

            // Cache Redis
            try
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            }
            catch { /* Nếu Redis lỗi thì vẫn trả dữ liệu */ }

            return Ok(result);
        }

        /// <summary>
        /// [GET] /api/public/products/{id}
        /// Lấy chi tiết sản phẩm theo ID (công khai)
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _dbContext.Products
                .Where(p => p.Id == id && !p.IsDeleted)
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
                    QrCode = p.QrCode,
                    Stock = p.Stock,
                    Slug = p.Slug
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        /// <summary>
        /// [POST] /api/public/products
        /// Thêm sản phẩm mới (Admin)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] ProductDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
                Slug = model.Slug,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        /// <summary>
        /// [PUT] /api/public/products/{id}
        /// Cập nhật sản phẩm (Admin)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditProduct(int id, [FromBody] ProductDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _dbContext.Products.FindAsync(id);
            if (product == null || product.IsDeleted)
                return NotFound();

            product.Name = model.Name;
            product.Price = model.Price;
            product.Description = model.Description;
            product.ThumbnailUrl = model.ThumbnailUrl;
            product.CategoryId = model.CategoryId;
            product.Stock = model.Stock;
            product.Rating = model.Rating;
            product.IsPromoted = model.IsPromoted;
            product.QrCode = model.QrCode;
            product.Slug = model.Slug;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(product);
        }

        /// <summary>
        /// [DELETE] /api/public/products/{id}
        /// Xóa mềm sản phẩm (Admin)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null || product.IsDeleted)
                return NotFound();

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
