using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class sp_GetTopProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE PROCEDURE sp_GetTopProducts @TopN INT = 5
        AS
        BEGIN
            SELECT TOP (@TopN) p.Id, p.Name, SUM(oi.Quantity) AS TotalSold
            FROM Products p
            JOIN OrderItems oi ON p.Id = oi.ProductId
            JOIN Orders o ON oi.OrderId = o.Id
            WHERE o.Status = 'Delivered' AND p.IsDeleted = 0
            GROUP BY p.Id, p.Name
            ORDER BY TotalSold DESC;
        END;
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetTopProducts;");
        }

    }
}
