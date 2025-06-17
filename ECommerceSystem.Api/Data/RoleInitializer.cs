using Microsoft.AspNetCore.Identity;
using ECommerceSystem.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ECommerceSystem.Api.Data
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            // Tạo vai trò Admin nếu chưa tồn tại
            Role adminRole = null;
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                adminRole = new Role { Name = "Admin", NormalizedName = "ADMIN" };
                var roleResult = await roleManager.CreateAsync(adminRole);
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Failed to create Admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                adminRole = await roleManager.FindByNameAsync("Admin");
            }

            // Tạo tài khoản Admin nếu chưa tồn tại
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = "admin",
                    Name = "adminUser",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DeviceToken = "default-token",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RoleId = adminRole.Id // Gán RoleId hợp lệ
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}