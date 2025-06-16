namespace ECommerceSystem.Shared.DTOs
{
    public class StatisticDTO
    {
        public List<object> Revenue { get; set; } // { Date, Value }
        public Dictionary<string, int> OrderCount { get; set; } // Thay List<int> thành Dictionary<string, int>
        public List<object> TopProducts { get; set; } // Thêm property mới
        public List<object> LowStock { get; set; } // Đã có từ trước
    }
}