using FoodOps.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Rider> Riders => Set<Rider>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<MenuItem>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Entity<Order>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        b.Entity<Order>().Property(x => x.CouponCode).HasMaxLength(30);
        b.Entity<OrderItem>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<Restaurant>().Property(x => x.Rating).HasPrecision(3, 2);

        b.Entity<OrderItem>()
            .HasOne(x => x.MenuItem)
            .WithMany()
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Order>()
            .HasOne(x => x.Restaurant)
            .WithMany()
            .HasForeignKey(x => x.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();

        b.Entity<Coupon>(e =>
        {
            e.Property(c => c.Code).HasMaxLength(30);
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Value).HasPrecision(18, 2);
            e.Property(c => c.MaxDiscount).HasPrecision(18, 2);
            e.Property(c => c.MinOrderAmount).HasPrecision(18, 2);
        });

        b.Entity<CouponRedemption>(e =>
        {
            // THE rule: one redemption per user per coupon, enforced by the database itself
            e.HasIndex(r => new { r.CouponId, r.UserId }).IsUnique();
            e.HasIndex(r => r.OrderId);
            e.HasOne(r => r.Coupon).WithMany().HasForeignKey(r => r.CouponId);
        });
    }
}