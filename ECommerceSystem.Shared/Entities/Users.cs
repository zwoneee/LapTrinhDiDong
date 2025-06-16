using Microsoft.AspNetCore.Identity;

namespace ECommerceSystem.Shared.Entities
{
    public class User : IdentityUser<int> // Use int as the key type
    {
        public string Name { get; set; }
        public string DeviceToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // Foreign key to custom Role
        public int RoleId { get; set; }
        public Role Role { get; set; }
    }

    public class Role : IdentityRole<int> // Use int as the key type
    {
        // One role has many users
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}