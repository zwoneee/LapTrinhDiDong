using ECommerceSystem.GUI.Apis;
using ECommerceSystem.GUI.Services;
using ECommerceSystem.GUI.Services.ECommerceSystem.GUI.Handlers;
using ECommerceSystem.Shared.Constants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Refit;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add MVC + HttpContextAccessor
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Custom services
builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<AuthRetryHandler>();

// ✅ Cấu hình xác thực: Cookie cho GUI, JWT cho Web API
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/Error";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

// ✅ Không cần AddJwtBearer nếu GUI không host API trực tiếp

// ✅ Đăng ký các HTTP client gửi JWT qua Authorization header
ConfigureRefit(builder.Services);

var app = builder.Build();

// Exception Page nếu đang dev
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // 👈 NÊN thêm để debug lỗi 500
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// ✅ Hàm cấu hình Refit + tự động gắn Authorization Bearer từ cookie
static void ConfigureRefit(IServiceCollection services)
{
    void SetHttpClient(HttpClient client, IServiceProvider sp)
    {
        client.BaseAddress = new Uri(AppConstants.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(20);

        var context = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
        var token = context?.Request?.Cookies["AuthToken"];

        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    // 👇 Updated to use a lambda to wrap the method group
    services.AddRefitClient<IAuthApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));

    services.AddRefitClient<IOrderApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));

    services.AddRefitClient<ICategoryApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));

    services.AddRefitClient<IProductApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));

    services.AddRefitClient<ICartApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));

    services.AddRefitClient<IUserApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));

    services.AddRefitClient<IAdminApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient((sp, client) => SetHttpClient(client, sp));
}