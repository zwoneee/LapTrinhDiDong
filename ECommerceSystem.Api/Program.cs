using AspNetCoreRateLimit;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Data.Mongo;
using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Api.Services;
<<<<<<< HEAD
using ECommerceSystem.Api.SwaggerConfig;
using Microsoft.AspNetCore.Authentication.JwtBearer;
=======
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
<<<<<<< HEAD


var builder = WebApplication.CreateBuilder(args);

// ==================== CẤU HÌNH DỊCH VỤ ====================

// Swagger + JWT UI Support
=======
using Role = ECommerceSystem.Shared.Entities.Role;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ECommerceSystem.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

// 🔍 Swagger
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ECommerceSystem.Api",
        Version = "v1"
    });

<<<<<<< HEAD
=======
    // ✅ Cấu hình bảo mật với JWT Bearer
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập JWT token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
<<<<<<< HEAD
        Scheme = "Bearer",
        BearerFormat = "JWT"
=======
        Scheme = "Bearer"
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
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
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
<<<<<<< HEAD

    c.OperationFilter<AuthenticationRequirementsOperationFilter>();
});

// SQL Server + EF Core
builder.Services.AddDbContext<WebDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔑 JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey missing"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});



// MongoDB
=======
});


// 💾 SQL Server & EF Core
builder.Services.AddDbContext<WebDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔐 Identity (dành cho quản lý người dùng và vai trò nếu dùng thêm)
// Configure Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<WebDBContext>()
.AddDefaultTokenProviders();

// 📦 MongoDB
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
var mongoConn = builder.Configuration["MongoDbSettings:ConnectionString"];
var dbName = builder.Configuration["MongoDbSettings:DatabaseName"];
builder.Services.AddSingleton(sp => new MongoDbContext(mongoConn, dbName));

<<<<<<< HEAD
// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("Redis:ConnectionString").Value;
});

// Rate Limiting
=======

// 🧠 Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

// 🔔 SignalR
builder.Services.AddSignalR();

// 🚫 Rate Limiting
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

<<<<<<< HEAD
// SignalR
builder.Services.AddSignalR();

// Repositories & Services
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<DataSyncService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", policy =>
    {
        policy.WithOrigins("https://localhost:7068", "http://localhost:5088")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers();

// ==================== BUILD APP ====================

var app = builder.Build();

// Swagger
=======

builder.Services.AddControllers();

//builder.Services.AddControllers(options =>
//{
//    var policy = new AuthorizationPolicyBuilder()
//        .RequireAuthenticatedUser()
//        .Build();
//    options.Filters.Add(new AuthorizeFilter(policy));
//});


// 🔑 Authentication - JWT Bearer
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
    });

// 🧩 Authorization (role-based đã tích hợp sẵn trong [Authorize(Roles = "...")])

// 💉 DI Repositories / Services
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<UserRepository>(); // cần cho AuthController
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("Redis:ConnectionString").Value;
});

// Cấu hình CORS để cho phép MVC gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", builder =>
    {
        builder.WithOrigins("https://localhost:7068", "http://localhost:5088")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Mongo
var mongoConfig = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
builder.Services.AddSingleton(sp =>
    new MongoDbContext(mongoConfig.ConnectionString, mongoConfig.DatabaseName));

// 🚀 Build app
var app = builder.Build();

// 📘 Swagger UI
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
<<<<<<< HEAD

// Khởi tạo Role & Admin
=======
// Khởi tạo vai trò
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleInitializer.InitializeAsync(services);
<<<<<<< HEAD
        await AdminInitializer.SeedAdminAsync(services);
=======
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
<<<<<<< HEAD
        logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu mặc định.");
    }
}

// ==================== MIDDLEWARE ====================

app.UseHttpsRedirection();
app.UseCors("AllowMvcApp");
app.UseIpRateLimiting();

app.UseRouting();

app.UseAuthentication(); // 🔐 PHẢI đứng trước Authorization
=======
        logger.LogError(ex, "Có lỗi xảy ra khi khởi tạo vai trò trong cơ sở dữ liệu.");
    }
}

// 🛡️ Middlewares
app.UseHttpsRedirection();
app.UseCors("AllowMvcApp");
app.UseIpRateLimiting();
app.UseRouting();

app.UseAuthentication(); // BẮT BUỘC đặt trước UseAuthorization
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

<<<<<<< HEAD
app.Run();
=======
app.Run();
>>>>>>> 0d97c07a047bc7a70a21b09e3ecefa7694131bbf
