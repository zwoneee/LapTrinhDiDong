using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class vm_CreateOrderRevenueView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE VIEW vw_OrderRevenue AS
        SELECT 
            CONVERT(date, CreatedAt) AS OrderDate,
            SUM(Total) AS DailyRevenue
        FROM Orders
        WHERE Status != 'Cancelled' AND IsDeleted = 0
        GROUP BY CONVERT(date, CreatedAt);
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_OrderRevenue;");
        }

    }
}
