using ECommerceSystem.Api.Data;
using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class WebDBContext : DbContext
{
    public WebDBContext(DbContextOptions<WebDBContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<PaymentReceipt> PaymentReceipts { get; set; }

    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<CartDetail> CartDetails { get; set; }
    public DbSet<Comment> Comments { get; set; }

    public DbSet<ProductRating> ProductRatings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 🔁 Precision cho các giá trị tiền
        modelBuilder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<CartDetail>().Property(c => c.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<PaymentReceipt>().Property(p => p.TotalAmount).HasPrecision(18, 2); // 👈 THÊM DÒNG NÀY

        // 🔁 Cấu hình quan hệ User - Role
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // 🔁 ShoppingCart - CartDetail
        modelBuilder.Entity<ShoppingCart>()
            .HasMany(c => c.CartDetails)
            .WithOne(d => d.ShoppingCart)
            .HasForeignKey(d => d.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartDetail>()
            .HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ProductRating relationship
        modelBuilder.Entity<ProductRating>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductRating>()
            .HasIndex(r => r.ProductId);

        // ✅ Soft delete filters
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted);
    }
}
