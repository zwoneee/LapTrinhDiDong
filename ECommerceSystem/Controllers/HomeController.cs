using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var suggestions = await client.GetFromJsonAsync<dynamic>("https://localhost:7068/api/suggestions");
            ViewBag.Suggestions = suggestions;
            return View();
        }
    }
}