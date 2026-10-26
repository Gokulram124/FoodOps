using System.Data;
using Dapper;
using FoodOps.Application.Abstractions;
using FoodOps.Domain.Enums;   // உங்க OrderStatus namespace; Ctrl+. மூலம் சரிசெய்யுங்க
using FoodOps.Infrastructure.Analytics;
using Microsoft.Data.Sqlite;

namespace FoodOps.Tests.Analytics;

// Each call to CreateConnection() opens a NEW connection (the repository disposes it),
// so we keep one "keeper" connection open to stop the shared in-memory database from vanishing.
public sealed class SqliteTestConnectionFactory : IDbConnectionFactory, IDisposable
{
    private readonly string _connectionString =
        $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";

    public SqliteConnection Keeper { get; }

    public SqliteTestConnectionFactory()
    {
        Keeper = new SqliteConnection(_connectionString);
        Keeper.Open();
    }

    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public void Dispose() => Keeper.Dispose();
}

public class OrderReportRepositoryTests : IDisposable
{
    private readonly SqliteTestConnectionFactory _factory = new();
    private readonly AnalyticsRepository _sut;

    public OrderReportRepositoryTests()
    {
        _sut = new AnalyticsRepository(_factory);
        var db = _factory.Keeper;

        db.Execute("""
            CREATE TABLE Restaurants (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
            CREATE TABLE MenuItems   (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
            CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY, RestaurantId INTEGER NOT NULL, Status INTEGER NOT NULL,
                OrderTime TEXT NOT NULL, TotalAmount INTEGER NOT NULL,
                DiscountAmount INTEGER NOT NULL, CouponCode TEXT NULL);
            CREATE TABLE OrderItems (
                Id INTEGER PRIMARY KEY, OrderId INTEGER NOT NULL, MenuItemId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL, Price INTEGER NOT NULL);
            """);

        db.Execute("INSERT INTO Restaurants (Id, Name) VALUES (1, 'Chennai Biryani Kadai')");
        db.Execute("""
            INSERT INTO MenuItems (Id, Name) VALUES
              (1, 'Chicken Biryani'), (2, 'Mutton Biryani'), (3, 'Chicken 65')
            """);

        // Order 1010: three lines, no coupon, total 750
        db.Execute("""
            INSERT INTO Orders (Id, RestaurantId, Status, OrderTime, TotalAmount, DiscountAmount, CouponCode)
            VALUES (1010, 1, @Delivered, '2026-10-12T05:59:53', 750, 0, NULL)
            """, new { Delivered = (int)OrderStatus.Delivered });
        db.Execute("""
            INSERT INTO OrderItems (Id, OrderId, MenuItemId, Quantity, Price) VALUES
              (1016, 1010, 1, 1, 220), (1017, 1010, 2, 1, 350), (1018, 1010, 3, 1, 180)
            """);

        // Order 1011: one line, with a coupon (660 - 100 = 560)
        db.Execute("""
            INSERT INTO Orders (Id, RestaurantId, Status, OrderTime, TotalAmount, DiscountAmount, CouponCode)
            VALUES (1011, 1, @Cancelled, '2026-10-16T13:01:36', 560, 100, 'WELCOME20')
            """, new { Cancelled = (int)OrderStatus.Cancelled });
        db.Execute("INSERT INTO OrderItems (Id, OrderId, MenuItemId, Quantity, Price) VALUES (1031, 1011, 1, 3, 220)");
    }

    [Fact]
    public async Task Returns_one_order_with_all_of_its_items()
    {
        var report = await _sut.GetOrderReportAsync(1010, CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(1010, report!.Id);
        Assert.Equal("Chennai Biryani Kadai", report.RestaurantName);
        Assert.Equal(
            new[] { "Chicken Biryani", "Mutton Biryani", "Chicken 65" },
            report.Items.Select(i => i.ItemName));
    }

    [Fact]
    public async Task Item_totals_minus_discount_match_the_order_total()
    {
        var report = await _sut.GetOrderReportAsync(1010, CancellationToken.None);

        Assert.NotNull(report);
        var itemsTotal = report!.Items.Sum(i => i.Quantity * i.Price);
        Assert.Equal(report.TotalAmount, itemsTotal - report.DiscountAmount);
    }

    [Fact]
    public async Task Does_not_mix_in_items_from_other_orders()
    {
        var first = await _sut.GetOrderReportAsync(1010, CancellationToken.None);
        var second = await _sut.GetOrderReportAsync(1011, CancellationToken.None);

        Assert.Equal(3, first!.Items.Count);
        Assert.Single(second!.Items);
    }

    [Fact]
    public async Task Maps_status_coupon_and_discount()
    {
        var report = await _sut.GetOrderReportAsync(1011, CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, report!.Status);
        Assert.Equal("WELCOME20", report.CouponCode);
        Assert.Equal(100, report.DiscountAmount);
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_order()
    {
        var report = await _sut.GetOrderReportAsync(424242, CancellationToken.None);

        Assert.Null(report);
    }

    public void Dispose() => _factory.Dispose();
}