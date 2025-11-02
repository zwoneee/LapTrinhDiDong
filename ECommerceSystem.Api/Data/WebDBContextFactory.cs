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
            // Build configuration from appsettings.json so EF tools use the same connection string
            var basePath = Directory.GetCurrentDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=TIEN;Database=Ecommerce;Trusted_Connection=True;TrustServerCertificate=True";

            var optionsBuilder = new DbContextOptionsBuilder<WebDBContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new WebDBContext(optionsBuilder.Options);
        }
    }
}
