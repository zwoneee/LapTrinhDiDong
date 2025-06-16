using ECommerceSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Dashboard()
        {
            var client = _httpClientFactory.CreateClient();
            var stats = await client.GetFromJsonAsync<StatisticDTO>("https://localhost:7068/api/admin/statistics?type=revenue&period=month");
            var inventory = await client.GetFromJsonAsync<dynamic>("https://localhost:7068/api/admin/inventory");
            var activities = await client.GetFromJsonAsync<dynamic>("https://localhost:7068/api/admin/user-activity");
            ViewBag.Stats = stats;
            ViewBag.Inventory = inventory;
            ViewBag.Activities = activities;
            return View();
        }
    }
}