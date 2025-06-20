using Microsoft.Extensions.DependencyInjection;
using ECommerceSystem.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.Api.Data
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<WebDBContext>();

            // Khởi tạo role 'Admin' nếu chưa có
            if (!await db.Roles.AnyAsync(r => r.Name == "Admin"))
            {
                db.Roles.Add(new Role { Name = "Admin" });
            }

            // Khởi tạo role 'User' nếu chưa có
            if (!await db.Roles.AnyAsync(r => r.Name == "User"))
            {
                db.Roles.Add(new Role { Name = "User" });
            }

            await db.SaveChangesAsync();
        }
    }
}
