using System.Net.Http.Headers;

namespace ECommerceSystem.GUI.Services
{
    public class AuthRetryHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthRetryHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Lấy token từ cookie
            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AuthToken"];

            // Nếu token tồn tại thì gắn vào Authorization header
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Tiếp tục chuỗi handler
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
