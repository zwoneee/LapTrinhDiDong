using ECommerceSystem.Shared.DTOs.Product;
using Refit;

namespace ECommerceSystem.GUI.Apis
{
    public interface IProductApi
    {
        [Get("/api/public/products")]
        Task<ApiResponse<object>> GetProductsAsync(
            [AliasAs("search")] string? search = null,
            [AliasAs("categoryId")] int? categoryId = null,
            [AliasAs("minPrice")] decimal? minPrice = null,
            [AliasAs("maxPrice")] decimal? maxPrice = null,
            [AliasAs("sortBy")] string? sortBy = null,
            [AliasAs("promotion")] bool? promotion = null,
            [AliasAs("page")] int page = 1,
            [AliasAs("pageSize")] int pageSize = 10
        );
    }
}
