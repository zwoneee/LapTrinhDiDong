////using ECommerceSystem.GUI.Apis;
////using ECommerceSystem.Shared.DTOs.Product;
////using Microsoft.AspNetCore.Mvc;
////using System.Text.Json;

////public class ProductController : Controller
////{
////    private readonly IProductApi _productApi;
////    private readonly ICategoryApi _categoryApi;

////    public ProductController(IProductApi productApi, ICategoryApi categoryApi)
////    {
////        _productApi = productApi;
////        _categoryApi = categoryApi;
////    }

////    public async Task<IActionResult> Index(
////     string? search, int? categoryId, decimal? minPrice, decimal? maxPrice,
////     string? sortBy, bool? promotion, int page = 1)
////    {
////        int pageSize = 9;

////        var response = await _productApi.GetProductsAsync(search, categoryId, minPrice, maxPrice, sortBy, promotion, page, pageSize);

////        if (!response.IsSuccessStatusCode || response.Content == null)
////        {
////            return View(new ProductListResponse { Products = new(), Total = 0, Page = page, PageSize = pageSize });
////        }

////        // ✅ Deserialize JsonElement về ProductListResponse
////        var jsonElement = (JsonElement)response.Content;
////        var result = JsonSerializer.Deserialize<ProductListResponse>(
////            jsonElement.GetRawText(),
////            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
////        );

////        // Giữ lại dữ liệu lọc
////        ViewBag.Search = search;
////        ViewBag.CategoryId = categoryId;
////        ViewBag.MinPrice = minPrice;
////        ViewBag.MaxPrice = maxPrice;
////        ViewBag.Promotion = promotion;
////        ViewBag.SortBy = sortBy;

////        // Lấy danh mục
////        var categories = await _categoryApi.GetAllAsync();


////        return View(result); // ✅ truyền đúng kiểu model
////    }

////}
//using ECommerceSystem.GUI.Apis;
//using ECommerceSystem.Shared.DTOs.Product;
//using Microsoft.AspNetCore.Mvc;

//public class ProductController : Controller
//{
//    private readonly IProductApi _productApi;
//    private readonly ICategoryApi _categoryApi;

//    public ProductController(IProductApi productApi, ICategoryApi categoryApi)
//    {
//        _productApi = productApi;
//        _categoryApi = categoryApi;
//    }

//    public async Task<IActionResult> Create()
//    {
//        ViewBag.Categories = await _categoryApi.GetAllAsync();
//        return View(new ProductDTO());
//    }

//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Create(ProductDTO model)
//    {
//        if (!ModelState.IsValid) return View(model);

//        await _productApi.CreateAsync(model);
//        return RedirectToAction("Index");
//    }

//    public async Task<IActionResult> Edit(int id)
//    {
//        var product = await _productApi.GetByIdAsync(id);
//        ViewBag.Categories = await _categoryApi.GetAllAsync();
//        return View(product);
//    }

//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Edit(ProductDTO model)
//    {
//        if (!ModelState.IsValid) return View(model);

//        await _productApi.UpdateAsync(model.Id, model);
//        return RedirectToAction("Index");
//    }

//    [HttpPost]
//    public async Task<IActionResult> Delete(int id)
//    {
//        await _productApi.DeleteAsync(id);
//        return RedirectToAction("Index");
//    }
//}
