using ECommerceSystem.GUI.Apis;
using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductApi _productApi;
        private readonly ICategoryApi _categoryApi;

        public ProductController(IProductApi productApi, ICategoryApi categoryApi)
        {
            _productApi = productApi;
            _categoryApi = categoryApi;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy, bool? promotion, int page = 1, int pageSize = 10)
        {
            var response = await _productApi.GetProductsAsync(search, categoryId, minPrice, maxPrice, sortBy, promotion, page, pageSize);
            var result = response.Content as ProductListResponse ?? new ProductListResponse();
            ViewBag.Categories = await _categoryApi.GetAllAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;
            ViewBag.Promotion = promotion;
            return View(result);
        }
    }
}