
namespace ECommerceSystem.Api.Services
{


    public class DataSyncService
    {
        private readonly WebDbContext _dbContext;
        private readonly MongoDbContext _mongoContext;

        public DataSyncService(AppDbContext dbContext, MongoDbContext mongoContext)
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
                var productDoc = new
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