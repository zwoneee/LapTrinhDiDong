using Microsoft.AspNetCore.Identity;
using ECommerceSystem.Shared.Entities; // namespace chứa User và Role

namespace ECommerceSystem.Api.Data
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();     // dùng Role tùy chỉnh
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();     // dùng User tùy chỉnh

            string[] roleNames = { "Admin", "Customer" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role { Name = roleName });
                }
            }

            // Tạo admin mặc định nếu chưa có
            string adminEmail = "admin@example.com";
            string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User { UserName = adminEmail, Email = adminEmail };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
