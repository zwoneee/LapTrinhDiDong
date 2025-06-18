using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Shared.DTOs;
using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly WebDBContext _dbContext;
        private readonly MongoDbContext _mongoContext;

        public AdminController(WebDBContext dbContext, MongoDbContext mongoContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mongoContext = mongoContext ?? throw new ArgumentNullException(nameof(mongoContext));
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(string type, string period)
        {
            try
            {
                var result = new StatisticDTO();

                switch (type?.ToLower())
                {
                    case "revenue":
                        var query = _dbContext.Orders
                            .Where(o => o.Status != "Cancelled" && !o.IsDeleted);
                        var grouped = period.ToLower() == "day" ? query.GroupBy(o => o.CreatedAt.Date) :
                                      period.ToLower() == "month" ? query.GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1)) :
                                      period.ToLower() == "year" ? query.GroupBy(o => new DateTime(o.CreatedAt.Year, 1, 1)) :
                                      query.GroupBy(o => o.CreatedAt.Date);
                        result.Revenue = await grouped
                            .Select(g => new { Date = g.Key, Value = g.Sum(o => o.Total) } as object)
                            .ToListAsync() ?? new List<object>();
                        break;

                    case "orders":
                        result.OrderCount = await _dbContext.Orders
                            .GroupBy(o => o.Status)
                            .Select(g => new { Status = g.Key, Count = g.Count() })
                            .ToDictionaryAsync(g => g.Status, g => g.Count) ?? new Dictionary<string, int>();
                        break;

                    case "top-products":
                        var orderItems = await _dbContext.OrderItems
                            .GroupBy(oi => oi.ProductId)
                            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(oi => oi.Quantity) })
                            .OrderByDescending(g => g.Quantity)
                            .Take(5)
                            .ToListAsync();
                        result.TopProducts = orderItems.Any()
                            ? orderItems.Join(_dbContext.Products,
                                oi => oi.ProductId,
                                p => p.Id,
                                (oi, p) => new { p.Id, p.Name, oi.Quantity } as object)
                            .ToList()
                            : new List<object>();
                        break;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var lowStockProducts = await _dbContext.Products
                    .Where(p => p.Stock > 0 && p.Stock <= 10 && !p.IsDeleted)
                    .Select(p => new { p.Id, p.Name, p.Stock } as object)
                    .ToListAsync() ?? new List<object>();
                return Ok(new { LowStock = lowStockProducts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("user-activity")]
        public async Task<IActionResult> GetUserActivity()
        {
            try
            {
                var logs = await _mongoContext.Logs.Find(_ => true).ToListAsync();
                var activities = logs
                    .GroupBy(l => l.Endpoint)
                    .Select(g => new { Action = g.Key, Count = g.Count() } as object)
                    .ToList() ?? new List<object>();

                return Ok(new { Activities = activities });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}