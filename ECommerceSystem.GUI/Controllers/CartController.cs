//using ECommerceSystem.Shared.DTOs.Product;
//using ECommerceSystem.Shared.Entities;
//using Microsoft.AspNetCore.Mvc;

//public class CartController : Controller
//{
//    private readonly ICartApi _cartApi;
//    private readonly IHttpContextAccessor _httpContextAccessor;

//    public CartController(ICartApi cartApi, IHttpContextAccessor httpContextAccessor)
//    {
//        _cartApi = cartApi;
//        _httpContextAccessor = httpContextAccessor;
//    }

//    private int GetCurrentUserId()
//    {
//        // Hoặc logic từ token/session
//        return int.Parse(User.FindFirst("userId")?.Value ?? "0");
//    }

//    public async Task<IActionResult> Index()
//    {
//        var cart = await _cartApi.GetCartAsync(GetCurrentUserId());
//        return View(cart);
//    }

//    [HttpPost]
//    public async Task<IActionResult> UpdateItem(CartItemDTO item)
//    {
//        await _cartApi.UpdateItemAsync(GetCurrentUserId(), item);
//        return RedirectToAction("Index");
//    }

//    [HttpPost]
//    public async Task<IActionResult> RemoveItem(int productId)
//    {
//        await _cartApi.RemoveItemAsync(GetCurrentUserId(), productId);
//        return RedirectToAction("Index");
//    }

//    [HttpPost]
//    public async Task<IActionResult> Clear()
//    {
//        await _cartApi.ClearCartAsync(GetCurrentUserId());
//        return RedirectToAction("Index");
//    }
//}
