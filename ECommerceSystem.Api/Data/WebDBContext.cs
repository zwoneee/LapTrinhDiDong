using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.Api.Data
{
    public class WebDBContext : IdentityDbContext<User, Role, int>

    {
        public WebDBContext(DbContextOptions<WebDBContext> options) : base(options) { }

        //public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        
            protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đổi tên bảng
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

            // Cấu hình lại mối quan hệ UserRoles để tránh multiple cascade paths
            modelBuilder.Entity<IdentityUserRole<int>>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<IdentityUserRole<int>>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IdentityUserRole<int>>()
                .HasOne<Role>()
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mối quan hệ giữa User và Role (tuỳ chỉnh riêng nếu bạn có RoleId trong User)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict); // cũng nên thêm để tránh lỗi tương tự

            // Soft delete filters
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        }

    }
}
