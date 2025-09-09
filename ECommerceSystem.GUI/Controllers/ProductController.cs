using ECommerceSystem.GUI.Apis;
using ECommerceSystem.GUI.Models;
using ECommerceSystem.Shared.DTOs.Category;
using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ECommerceSystem.GUI.Controllers
{
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

        private async Task<List<CategoryDTO>> GetCachedCategoriesAsync()
        {
            if (!_cache.TryGetValue(CategoriesCacheKey, out List<CategoryDTO> categories))
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
                var response = await _productApi.GetProductsAsync(searchTerm, categoryId, null, null, null, null, page, pageSize);
                var products = response?.Products ?? new List<ProductDTO>();

                ViewBag.Categories = await GetCachedCategoriesAsync();
                ViewBag.SearchTerm = searchTerm;
                ViewBag.CategoryId = categoryId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((response?.Total ?? 0) / (double)pageSize);

                return View(products);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải danh sách sản phẩm: {ex.Message}";
                ViewBag.Categories = new List<CategoryDTO>();
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                return View(new List<ProductDTO>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await GetCachedCategoriesAsync();
            return View(new ProductFormModel { Product = new ProductDTO() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormModel form)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetCachedCategoriesAsync();
                return View(form);
            }

            var dto = form.Product;

            if (form.ThumbnailFile != null && form.ThumbnailFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(form.ThumbnailFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await form.ThumbnailFile.CopyToAsync(stream);
                }

                dto.ThumbnailUrl = "/uploads/" + fileName;
            }

            await _productApi.CreateAsync(dto);
            TempData["Success"] = "Sản phẩm đã được tạo thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
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
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var product = await _productApi.GetByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction(nameof(Index));
                }

                // Lấy danh sách sản phẩm cùng danh mục
                var relatedProductResponse = await _productApi.GetProductsAsync(
                    search: null,
                    categoryId: product.CategoryId,
                    minPrice: null,
                    maxPrice: null,
                    sortBy: null,
                    promotion: null,
                    page: 1,
                    pageSize: 10 // lấy 10 để lọc, có thể chọn top 4 sau
                );

                // Loại bỏ sản phẩm hiện tại ra khỏi danh sách liên quan
                var relatedProducts = relatedProductResponse.Products
                                        .Where(p => p.Id != product.Id)
                                        .Take(4)
                                        .ToList();

                ViewBag.RelatedProducts = relatedProducts;
                ViewBag.Categories = await GetCachedCategoriesAsync();

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải chi tiết sản phẩm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
