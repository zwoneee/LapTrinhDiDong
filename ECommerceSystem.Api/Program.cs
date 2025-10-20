using AspNetCoreRateLimit;
using EcommerceSystem.API.Data.Repositories;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Api.Repositories;
using ECommerceSystem.Api.Services;
using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//#region Swagger (OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ECommerceSystem.Api",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Dán JWT token vào đây (không cần ghi chữ Bearer):",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "Bearer",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
//#endregion

#region Database & MongoDB
builder.Services.AddDbContext<WebDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}
#endregion

#region SignalR
builder.Services.AddSignalR();
#endregion

#region Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
#endregion

#region Authentication - JWT
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var rawToken = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(rawToken) && !rawToken.StartsWith("Bearer "))
                {
                    context.Token = rawToken;
                }

                return Task.CompletedTask;
            }
        };
    });
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", policy =>
    {
        policy.WithOrigins("https://localhost:7068", "http://localhost:5088")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Cho phép gửi cookie
    });
});
#endregion

#region Dependency Injection
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddControllers();
#endregion

var app = builder.Build();

#region Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ✅ nếu bạn có file ảnh sản phẩm, nên bật cái này

app.UseCors("AllowMvcApp");

app.UseIpRateLimiting();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
#endregion

#region Init Roles & Admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleInitializer.InitializeAsync(services);
        await AdminInitializer.SeedAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi khởi tạo vai trò.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleInitializer.InitializeAsync(services);
    await UserInitializer.InitializeAsync(services);
}

#endregion

app.Run();
