using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class WebDBContext : IdentityDbContext<User, Role, int>
{
    public WebDBContext(DbContextOptions<WebDBContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Đổi tên bảng Identity
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

        // Cấu hình UserRoles
        modelBuilder.Entity<IdentityUserRole<int>>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<IdentityUserRole<int>>()
            .HasOne<User>().WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IdentityUserRole<int>>()
            .HasOne<Role>().WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);

        // Quan hệ User-Role
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cấu hình Order-OrderItem
        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .IsRequired(false);

        // Soft delete filters
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(oi => !oi.IsDeleted);

        // Cấu hình decimal
        modelBuilder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
    }
}
