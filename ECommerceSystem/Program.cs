var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Admin"))
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            await next();
        }
        else
        {
            context.Response.Redirect("/admin/dashboard");
        }
    }
    else if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/products");
    }
    else
    {
        context.Response.Redirect("/products");
    }
    await next();
});
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
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
