using ECommerceSystem.Shared.DTOs.Product;
using Refit;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Apis
{
    public interface IProductApi
    {
        [Get("/api/public/products")]
        Task<ProductListResponse> GetProductsAsync(
            [AliasAs("search")] string? search,
            [AliasAs("categoryId")] int? categoryId,
            [AliasAs("minPrice")] decimal? minPrice,
            [AliasAs("maxPrice")] decimal? maxPrice,
            [AliasAs("sortBy")] string? sortBy,
            [AliasAs("promotion")] bool? promotion,
            [AliasAs("page")] int page = 1,
            [AliasAs("pageSize")] int pageSize = 10);

        [Get("/api/public/products/{id}")]
        Task<ProductDTO> GetByIdAsync(int id);

        [Post("/api/public/products")]
        Task CreateAsync([Body] ProductDTO dto);

        [Put("/api/public/products/{id}")]
        Task UpdateAsync(int id, [Body] ProductDTO dto);

        [Delete("/api/public/products/{id}")]
        Task DeleteAsync(int id);
    }
}
