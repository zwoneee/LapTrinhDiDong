using ECommerceSystem.GUI.Apis;
using ECommerceSystem.GUI.Services;
using ECommerceSystem.GUI.Services.ECommerceSystem.GUI.Handlers;
using ECommerceSystem.Shared.Constants;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
ConfigureRefit(builder.Services);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<AuthRetryHandler>();
// ✅ thêm dòng này

//services.AddRefitClient<IUserApi>()
//    .AddHttpMessageHandler<AuthRetryHandler>() // ✅ gắn handler
//    .ConfigureHttpClient(SetHttpClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}")
    .WithStaticAssets();


app.Run();
// ✅ Định nghĩa phương thức ConfigureRefit sau app.Run()
static void ConfigureRefit(IServiceCollection services)
{
    services.AddRefitClient<IAuthApi>()
        .AddHttpMessageHandler<AuthRetryHandler>()
        .ConfigureHttpClient(SetHttpClient);


    void SetHttpClient(HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri(AppConstants.ApiBaseUrl); // ✅ nhớ cập nhật AppConstants
        httpClient.Timeout = TimeSpan.FromSeconds(20);
        Console.WriteLine($"[Refit] BaseAddress set to: {httpClient.BaseAddress}");
    }



}