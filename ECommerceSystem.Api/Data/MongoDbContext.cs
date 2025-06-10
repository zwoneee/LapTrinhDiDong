using MongoDB.Driver;
using ECommerceSystem.Shared.Entities;
using ECommerceSystem.Shared.Documents;

namespace ECommerceSystem.Api.Data.Mongo
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<GuestSession> GuestSessions => _database.GetCollection<GuestSession>("GuestSessions");
        public IMongoCollection<UserPreference> Preferences => _database.GetCollection<UserPreference>("Preferences");
        public IMongoCollection<Log> Logs => _database.GetCollection<Log>("Logs");
        public IMongoCollection<UserLocation> UserLocations => _database.GetCollection<UserLocation>("UserLocations");
        public IMongoCollection<Promotion> Promotions => _database.GetCollection<Promotion>("Promotions");
        public IMongoCollection<ProductDocument> Products => _database.GetCollection<ProductDocument>("Products"); // Thêm dòng này
    }
}