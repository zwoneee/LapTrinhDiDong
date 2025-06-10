using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ECommerceSystem.Api.Data
{
    public class WebDBContextFactory : IDesignTimeDbContextFactory<WebDBContext>
    {
        public WebDBContext CreateDbContext(string[] args)
        {
            // Tìm file appsettings.json ở thư mục chứa csproj
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<WebDBContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);

            return new WebDBContext(optionsBuilder.Options);
        }
    }
}
