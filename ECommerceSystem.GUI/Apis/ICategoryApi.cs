using ECommerceSystem.Shared.DTOs.Category;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Apis
{
    public interface ICategoryApi
    {
        [Get("/api/public/categories")]
        Task<List<CategoryDTO>> GetAllAsync();

        [Get("/api/public/categories/{id}")]
        Task<CategoryDTO> GetByIdAsync(int id);

        [Post("/api/public/categories")]
        Task CreateAsync([Body] CategoryDTO dto);

        [Put("/api/public/categories/{id}")]
        Task UpdateAsync(int id, [Body] CategoryDTO dto);

        [Delete("/api/public/categories/{id}")]
        Task DeleteAsync(int id);
    }
}
