using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Shared.DTOs.User;
using MongoDB.Driver;

namespace ECommerceSystem.Api.Services
{
    public class AnalyticsService
    {
        private readonly MongoDbContext _mongoContext;

        public AnalyticsService(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public List<SequenceDTO> RunPrefixSpan()
        {
            var preferences = _mongoContext.Preferences.Find(_ => true).ToList();
            // Placeholder cho logic PrefixSpan (cần triển khai chi tiết)
            var sequences = new List<SequenceDTO>();
            // Ví dụ: Lấy chuỗi tìm kiếm
            foreach (var pref in preferences)
            {
                sequences.Add(new SequenceDTO { Sequence = string.Join(",", pref.SearchHistory) });
            }
            return sequences;
        }
    }
}
