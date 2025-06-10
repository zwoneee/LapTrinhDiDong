namespace ECommerceSystem.Shared.DTOs
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string DeviceToken { get; set; }
        public bool IsDeleted { get; set; }
    }
}

