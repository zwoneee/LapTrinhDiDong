using ECommerceSystem.GUI.Apis;
using ECommerceSystem.Shared.DTOs.Category;
using ECommerceSystem.Shared.DTOs.Product;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Controllers
{
    //[Authorize(Roles = "Admin")]
   // [Route("[controller]/[action]")]
    public class ProductController : Controller
    {
        private readonly IProductApi _productApi;
        private readonly ICategoryApi _categoryApi;
        private readonly IMemoryCache _cache;
        private const string CategoriesCacheKey = "CategoriesList";

        public ProductController(IProductApi productApi, ICategoryApi categoryApi, IMemoryCache cache)
        {
            _productApi = productApi;
            _categoryApi = categoryApi;
            _cache = cache;
        }

        private async Task<object> GetCachedCategoriesAsync()
        {
            if (!_cache.TryGetValue(CategoriesCacheKey, out var categories))
            {
                categories = await _categoryApi.GetAllAsync();
                var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));
                _cache.Set(CategoriesCacheKey, categories, cacheOptions);
            }
            return categories;
        }
       
        public async Task<IActionResult> Index(string searchTerm, int? categoryId, int page = 1, int pageSize = 10)
        {
            try
            {
                var productsResponse = await _productApi.GetProductsAsync(searchTerm, categoryId, null, null, null, null, page, pageSize);

                // Bảo vệ: nếu response hoặc products null, tạo danh sách rỗng
                var products = productsResponse?.Products ?? new List<ProductDTO>();

                // Cache danh mục an toàn
                var cachedCategories = await GetCachedCategoriesAsync() ?? new List<CategoryDTO>();
                ViewBag.Categories = cachedCategories;

                ViewBag.SearchTerm = searchTerm;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((productsResponse?.Total ?? 0) / (double)pageSize);

                return View(products);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải danh sách sản phẩm: {ex.Message}";
                ViewBag.Categories = new List<CategoryDTO>(); // tránh lỗi null
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                return View(new List<ProductDTO>());
            }
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await GetCachedCategoriesAsync();
            return View(new ProductDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetCachedCategoriesAsync();
                return View(dto);
            }

            try
            {
                await _productApi.CreateAsync(dto);
                TempData["Success"] = "Sản phẩm đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tạo sản phẩm: {ex.Message}";
                ViewBag.Categories = await GetCachedCategoriesAsync();
                return View(dto);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _productApi.GetByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                ViewBag.Categories = await GetCachedCategoriesAsync();
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải sản phẩm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetCachedCategoriesAsync();
                return View(dto);
            }

            try
            {
                await _productApi.UpdateAsync(id, dto);
                TempData["Success"] = "Sản phẩm đã được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật sản phẩm: {ex.Message}";
                ViewBag.Categories = await GetCachedCategoriesAsync();
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productApi.GetByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                await _productApi.DeleteAsync(id);
                TempData["Success"] = "Sản phẩm đã được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa sản phẩm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}