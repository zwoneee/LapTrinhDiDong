using AspNetCoreRateLimit;
using EcommerceSystem.API.Data.Repositories;
using EcommerceSystem.API.Services;
using ECommerceSystem.Api.Data;
using ECommerceSystem.Api.Data.Repositories;
using ECommerceSystem.Api.Hubs;
using ECommerceSystem.Api.Repositories;
using ECommerceSystem.Api.Services;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using Role = ECommerceSystem.Shared.Entities.Role;

var builder = WebApplication.CreateBuilder(args);

#region === Kestrel / URLs ===
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5106); // ✅ Cho phép mọi IP truy cập http://<IP máy bạn>:5106
    options.ListenAnyIP(7068, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});
#endregion

#region === SERVICES ===

// ✅ Database
builder.Services.AddDbContext<WebDBContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// ✅ Identity
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<Role>()
.AddEntityFrameworkStores<WebDBContext>()
.AddSignInManager<SignInManager<User>>()
.AddUserManager<UserManager<User>>()
.AddRoleManager<RoleManager<Role>>()
.AddDefaultTokenProviders();

// ✅ Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}

// ✅ SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatConnectionManager>();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// ✅ Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ✅ Authentication (JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = builder.Configuration["Jwt:SecretKey"]
            ?? throw new Exception("Missing Jwt:SecretKey in appsettings.json");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        // ✅ Cho phép gửi token qua query cho SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/chathub") || path.StartsWithSegments("/commenthub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// ✅ Authorization
builder.Services.AddAuthorization();

// ✅ CORS — Cho phép mọi nguồn để Flutter truy cập
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
                "http://127.0.0.1:7171",
                "https://localhost:7068", // API
                "http://localhost:7068",
                "https://localhost:7171", // GUI
                "http://localhost:7171",
                "http://localhost:5173",
                "http://localhost:8080",
                "http://127.0.0.1:5173",
                "http://10.0.2.2", 
                "http://192.168.1.5:5106" 
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


// ✅ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Dependency Injection
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<DataSyncService>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

// ✅ Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EcommerceSystem.API", Version = "v1" });

    // 🔒 Thêm định nghĩa cho Bearer Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header.  
                      Nhập vào đây token của bạn (VD: Bearer eyJhbGciOi...).  
                      Lưu ý phải có chữ **Bearer** và khoảng trắng phía trước token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // 🔐 Áp dụng yêu cầu xác thực mặc định cho các endpoint
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // ✅ Thêm OperationFilter để tự động yêu cầu xác thực nếu có [Authorize]
    c.OperationFilter<ECommerceSystem.Api.SwaggerConfig.AuthenticationRequirementsOperationFilter>();
});


#endregion

var app = builder.Build();

#region === MIDDLEWARE ===
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

// ✅ KHÔNG ép HTTPS trong development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

app.UseIpRateLimiting();
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ✅ Map Controllers + SignalR
app.MapControllers();
app.MapHub<ChatHub>("/chathub");
app.MapHub<CommentHub>("/commenthub");

#endregion

#region === SEED DỮ LIỆU MẶC ĐỊNH ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await AdminInitializer.SeedRolesAndAdminAsync(services);
        await UserInitializer.SeedDefaultUserAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Lỗi khi khởi tạo dữ liệu mặc định (roles/users).");
    }
}
#endregion

app.Run();
