USE [ECommereceSystem]
GO

-- Xóa dữ liệu cũ nếu có
DELETE FROM [dbo].[Products];
GO

-- Thêm sản phẩm mẫu
INSERT INTO [dbo].[Products] (
    [Name], [Price], [Description], [ThumbnailUrl],
    [CategoryId], [Stock], [Rating], [IsPromoted], [QrCode],
    [CreatedAt], [UpdatedAt], [IsDeleted], [Slug]
)
VALUES
(N'Áo thun trắng', 199000, N'Áo thun trắng cổ tròn', N'https://example.com/images/shirt.jpg',
 1, 100, 4.5, 1, N'', GETDATE(), NULL, 0, N'ao-thun-trang'),

(N'Giày thể thao', 799000, N'Giày chạy bộ phong cách thể thao', N'https://example.com/images/shoes.jpg',
 2, 50, 4.8, 1, N'', GETDATE(), NULL, 0, N'giay-the-thao'),

(N'Balo laptop', 499000, N'Balo chống nước đựng laptop 15 inch', N'https://example.com/images/backpack.jpg',
 3, 75, 4.2, 0, N'', GETDATE(), NULL, 0, N'balo-laptop');
GO