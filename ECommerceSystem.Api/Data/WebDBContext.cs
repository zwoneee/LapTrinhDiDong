using ECommerceSystem.Shared.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.Api.Data
{
    public class WebDBContext : IdentityDbContext<User, Role, int>
    {
        public WebDBContext(DbContextOptions<WebDBContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PaymentReceipt> PaymentReceipts { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ProductRating> ProductRatings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity sẽ tự sinh bảng AspNetUsers, AspNetRoles, AspNetUserRoles

            // Precision cho các giá trị tiền
            modelBuilder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<CartDetail>().Property(c => c.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<PaymentReceipt>().Property(p => p.TotalAmount).HasPrecision(18, 2);

            // ShoppingCart - CartDetail
            modelBuilder.Entity<ShoppingCart>()
                .HasMany(c => c.CartDetails)
                .WithOne(d => d.ShoppingCart)
                .HasForeignKey(d => d.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartDetail - Product relationship
            modelBuilder.Entity<CartDetail>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // ChatMessage - User relationships
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.ToUser)
                .WithMany()
                .HasForeignKey(c => c.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.FromUser)
                .WithMany()
                .HasForeignKey(c => c.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductRating relationship
            modelBuilder.Entity<ProductRating>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductRating>()
                .HasIndex(r => r.ProductId);

            // Soft delete filters
            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted);

            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
