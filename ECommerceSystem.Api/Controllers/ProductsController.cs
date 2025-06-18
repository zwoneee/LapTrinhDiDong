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

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/public/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly WebDBContext _dbContext;
        private readonly MongoDbContext _mongoContext;
        private readonly IDistributedCache _cache;

        public ProductsController(WebDBContext dbContext, MongoDbContext mongoContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _mongoContext = mongoContext;
            _cache = cache;
        }

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
                    QrCode = p.QrCode
                })
                .ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                await _mongoContext.Preferences.InsertOneAsync(new UserPreference
                {
                    GuestId = Guid.NewGuid().ToString(),
                    SearchHistory = new List<string> { search },
                    Timestamp = DateTime.UtcNow
                });
            }

            var result = new { total, page, pageSize, products };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(result);
        }
    }
}
