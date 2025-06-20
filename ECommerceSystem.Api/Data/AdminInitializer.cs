using ECommerceSystem.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace ECommerceSystem.Api.Data
{
    public static class AdminInitializer
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<WebDBContext>();

            // Tạo role 'Admin' nếu chưa có
            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                adminRole = new Role { Name = "Admin" };
                db.Roles.Add(adminRole);
                await db.SaveChangesAsync();
            }

            // Tạo user 'admin' nếu chưa có
            var adminUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
            if (adminUser == null)
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                adminUser = new User
                {
                    UserName = "admin",
                    Name = "Admin",
                    Email = "admin@gmail.com",
                    RoleId = adminRole.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    PasswordHash = hashedPassword,
                    DeviceToken = ""
                };
                db.Users.Add(adminUser);
                await db.SaveChangesAsync();
            }
        }
    }
}
