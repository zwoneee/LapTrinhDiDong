// Các thư viện cần thiết
using AspNetCoreRateLimit;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Api.Services;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

// Khởi tạo builder
var builder = WebApplication.CreateBuilder(args);

#region Swagger (OpenAPI)
// Cấu hình Swagger để hiển thị tài liệu API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ECommerceSystem.Api",
        Version = "v1"
    });

    // Cho phép nhập token thuần (không cần "Bearer ")
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Chỉ dán JWT token vào đây (KHÔNG cần thêm 'Bearer ' ở đầu)",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Áp dụng xác thực cho tất cả các API
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
#endregion


#region Database & MongoDB
// Cấu hình Entity Framework với SQL Server
builder.Services.AddDbContext<WebDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình MongoDB và inject MongoDbContext vào DI container
var mongoConfig = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
builder.Services.AddSingleton(sp =>
    new MongoDbContext(mongoConfig.ConnectionString, mongoConfig.DatabaseName));
#endregion

#region Redis
// Cấu hình Redis nếu có ConnectionString
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Kết nối Redis thông qua StackExchange.Redis
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    // Thiết lập caching sử dụng Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}
#endregion

#region SignalR
// Kích hoạt dịch vụ SignalR cho thông báo realtime
builder.Services.AddSignalR();
#endregion

#region Rate Limiting
// Cấu hình giới hạn tần suất gọi API theo IP
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
#endregion

#region Authentication - JWT
// Cấu hình xác thực JWT
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

        // Xử lý token thủ công - KHÔNG gắn "Bearer"
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var rawToken = context.Request.Headers["Authorization"].FirstOrDefault();

                if (!string.IsNullOrEmpty(rawToken) && !rawToken.StartsWith("Bearer "))
                {
                    // Set lại token trực tiếp (không cần chữ "Bearer ")
                    context.Token = rawToken;
                }

                return Task.CompletedTask;
            }
        };
    });

#endregion

#region CORS
// Cho phép truy cập từ các ứng dụng MVC/Web khác (frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", builder =>
    {
        builder.WithOrigins("https://localhost:7068", "http://localhost:5088")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Cho phép gửi cookie/token
    });
});
#endregion

#region Dependency Injection
// Đăng ký các dịch vụ và repository sử dụng DI
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<UserRepository>();

// Kích hoạt API Controller
builder.Services.AddControllers();
#endregion

// Tạo app từ builder đã cấu hình
var app = builder.Build();

#region Middleware
if (app.Environment.IsDevelopment())
{
    // Hiển thị lỗi và Swagger khi ở môi trường dev
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Middleware xử lý lỗi khi ở môi trường production
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Các middleware cần thiết cho API
app.UseHttpsRedirection();
app.UseCors("AllowMvcApp");
app.UseIpRateLimiting();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map các route API và SignalR Hub
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
#endregion

#region Khởi tạo vai trò và tài khoản admin mặc định
// Tạo vai trò và người dùng quản trị mặc định nếu chưa có
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
#endregion

// Chạy ứng dụng
app.Run();
