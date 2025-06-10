using Microsoft.AspNetCore.Mvc;
using ECommerceSystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Shared.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly WebDBContext _dbContext;
        private readonly MongoDbContext _mongoContext;

        public AdminController(WebDBContext dbContext, MongoDbContext mongoContext)
        {
            _dbContext = dbContext;
            _mongoContext = mongoContext;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(string type, string period)
        {
            var result = new StatisticDTO();

            switch (type?.ToLower())
            {
                case "revenue":
                    var query = _dbContext.Orders
                        .Where(o => o.Status != "Cancelled" && !o.IsDeleted)
                        .GroupBy(o =>
                            period.ToLower() == "day" ? o.CreatedAt.Date :
                            period.ToLower() == "month" ? new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1) :
                            period.ToLower() == "year" ? new DateTime(o.CreatedAt.Year, 1, 1) :
                            o.CreatedAt.Date);
                    result.Revenue = await query
                        .Select(g => new { Date = g.Key, Value = g.Sum(o => o.Total) })
                        .Cast<object>()
                        .ToListAsync();
                    break;

                case "orders":
                    result.OrderCount = await _dbContext.Orders
                        .GroupBy(o => o.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(g => g.Status, g => g.Count);
                    break;

                case "top-products":
                    result.TopProducts = await _dbContext.OrderItems
                        .GroupBy(oi => oi.ProductId)
                        .Select(g => new { ProductId = g.Key, Quantity = g.Sum(oi => oi.Quantity) })
                        .OrderByDescending(g => g.Quantity)
                        .Take(5)
                        .Join(_dbContext.Products,
                            oi => oi.ProductId,
                            p => p.Id,
                            (oi, p) => new { p.Id, p.Name, oi.Quantity })
                        .Cast<object>()
                        .ToListAsync();
                    break;
            }

            return Ok(result);
        }


        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            var lowStockProducts = await _dbContext.Products
                .Where(p => p.Stock > 0 && p.Stock <= 10 && !p.IsDeleted)
                .Select(p => new { p.Id, p.Name, p.Stock })
                .ToListAsync();
            return Ok(new { LowStock = lowStockProducts });
        }
    }
}