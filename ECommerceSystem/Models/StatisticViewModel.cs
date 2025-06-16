namespace ECommerceSystem.Models
{
    public class StatisticViewModel
    {
        public List<object> Revenue { get; set; }
        public Dictionary<string, int> OrderCount { get; set; }
        public List<object> TopProducts { get; set; }
        public List<object> LowStock { get; set; }
        public List<object> Activities { get; set; }
    }
}
