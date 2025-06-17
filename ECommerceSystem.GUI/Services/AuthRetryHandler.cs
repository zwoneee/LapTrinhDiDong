namespace ECommerceSystem.GUI.Services
{
    using Microsoft.AspNetCore.Http;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    namespace ECommerceSystem.GUI.Handlers
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


}
