using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.GUI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        
        public IActionResult Index()
        {
            var model = new { TotalUsers = 100, TotalProducts = 50, TotalOrders = 200 };
            return View(model);
        }
    }
}
