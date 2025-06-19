using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Identity;

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

        // Tạo vai trò User nếu chưa tồn tại
        if (!await roleManager.RoleExistsAsync("User"))
        {
            var userRole = new Role { Name = "User", NormalizedName = "USER" };
            var userRoleResult = await roleManager.CreateAsync(userRole);
            if (!userRoleResult.Succeeded)
            {
                throw new Exception($"Failed to create User role: {string.Join(", ", userRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        // Tạo tài khoản Admin nếu chưa tồn tại
        var adminUserName = "admin";
        var adminEmail = "admin@gmail.com";
        var adminUser = await userManager.FindByNameAsync(adminUserName); // ✅ kiểm tra theo UserName

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminUserName,
                Name = "adminUser",
                Email = adminEmail,
                EmailConfirmed = true,
                DeviceToken = "default-token",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RoleId = adminRole.Id
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
        else
        {
            // (Tùy chọn) đảm bảo đã gán role nếu thiếu
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

    }
}
