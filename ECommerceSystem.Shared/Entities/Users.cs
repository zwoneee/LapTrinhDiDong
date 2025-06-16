using ECommerceSystem.Shared.Constants;

namespace ECommerceSystem.Shared.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        // Khóa ngoại
        public int RoleId { get; set; }

        // Quan hệ 1-1
        public Role Role { get; set; }
        public string DeviceToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        
    }
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Một role có nhiều user
        public ICollection<User> Users { get; set; } = new List<User>();
    }

}