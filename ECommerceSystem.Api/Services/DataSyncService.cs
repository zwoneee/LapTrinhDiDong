using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Shared.Documents; // Thêm namespace này
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;

namespace ECommerceSystem.Api.Services
{
    public class DataSyncService
    {
        private readonly WebDBContext _dbContext; 
        private readonly MongoDbContext _mongoContext;

        public DataSyncService(WebDBContext dbContext, MongoDbContext mongoContext)
        {
            _dbContext = dbContext;
            _mongoContext = mongoContext;
        }

        public async Task SyncProductsToMongo()
        {
            var products = await _dbContext.Products
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            foreach (var product in products)
            {
                var productDoc = new ProductDocument
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    LastSynced = DateTime.UtcNow
                };

                await _mongoContext.Products.ReplaceOneAsync(
                    p => p.ProductId == product.Id,
                    productDoc,
                    new ReplaceOptions { IsUpsert = true });
            }
        }
    }
}