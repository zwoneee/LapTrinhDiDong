namespace ECommerceSystem.Shared.DTOs
{
    public class StatisticDTO
    {
        public List<object> Revenue { get; set; } // { Date, Value }
        public List<int> OrderCount { get; set; }
    }
}

