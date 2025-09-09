using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.Api.Data
{
    public static class UserInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<WebDBContext>();

            // Reset lại IDENTITY về 0
            await db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Users', RESEED, 0)");

            await db.SaveChangesAsync();
        }
    }

}
