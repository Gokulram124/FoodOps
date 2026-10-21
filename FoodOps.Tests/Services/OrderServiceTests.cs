using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Orders;
using FoodOps.Application.Interfaces;
using FoodOps.Application.Notifications;
using FoodOps.Application.OrderRules;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Repositories;
using FoodOps.Infrastructure.Services;
using FoodOps.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FoodOps.Application.Coupons;
using Microsoft.Extensions.Time.Testing;
namespace FoodOps.Tests.Services;

// xUnit creates a NEW instance of this class for every test, so every test gets its own fresh database.
public class OrderServiceTests : IDisposable
{
    private readonly TestDb _test = new();
    private readonly Mock<IOrderNotifier> _notifier = new();
    private readonly OrderService _sut;   // "system under test"

    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _customer = Guid.NewGuid();

    private readonly Restaurant _kadai;
    private readonly Restaurant _closed;
    private readonly Restaurant _other;
    private readonly MenuItem _biryani;     // 200
    private readonly MenuItem _coffee;      // 40
    private readonly MenuItem _soldOut;
    private readonly MenuItem _otherItem;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly CouponRedeemer _redeemer;

    public OrderServiceTests()
    {
        _kadai = new Restaurant { Name = "Test Kadai", City = "Chennai", OwnerId = _owner, IsOpen = true };
        _kadai.MenuItems.Add(_biryani = new MenuItem { Name = "Biryani", Price = 200, Category = "Main" });
        _kadai.MenuItems.Add(_coffee = new MenuItem { Name = "Coffee", Price = 40, Category = "Drinks" });
        _kadai.MenuItems.Add(_soldOut = new MenuItem { Name = "Sold out", Price = 10, Category = "Main", IsAvailable = false });

        _closed = new Restaurant { Name = "Closed Cafe", City = "Salem", OwnerId = _owner, IsOpen = false };
        _closed.MenuItems.Add(new MenuItem { Name = "Tea", Price = 20, Category = "Drinks" });

        _other = new Restaurant { Name = "Other Place", City = "Madurai", OwnerId = Guid.NewGuid(), IsOpen = true };
        _other.MenuItems.Add(_otherItem = new MenuItem { Name = "Dosa", Price = 60, Category = "Tiffin" });

        _test.Db.Restaurants.AddRange(_kadai, _closed, _other);
        _test.Db.SaveChanges();
        _redeemer = new CouponRedeemer(_test.Db,
    new IDiscountStrategy[] { new PercentDiscountStrategy(), new FlatDiscountStrategy() }, _clock);
        _sut = new OrderService(
    new UnitOfWork(_test.Db),
    new IStatusTransitionRule[] { new CustomerCancelRule(), new RestaurantFlowRule() },
    Mock.Of<IOrderNotificationFactory>(),
    _notifier.Object,
    NullLogger<OrderService>.Instance,
                           new StockService(_test.Db, _notifier.Object, NullLogger<StockService>.Instance),_redeemer);
    }

    public void Dispose() => _test.Dispose();

    // If your DTO names/constructors differ, this is the ONLY place to adapt.
    private static CreateOrderDto Request(int restaurantId, params (int itemId, int qty)[] lines) =>
    new(restaurantId, lines.Select(l => new OrderItemRequest(l.itemId, l.qty)).ToList());

    // ---------------- PlaceOrderAsync ----------------

    [Fact]
    public async Task Placing_an_order_saves_it_with_total_calculated_from_database_prices()
    {
        var result = await _sut.PlaceOrderAsync(
            Request(_kadai.Id, (_biryani.Id, 2), (_coffee.Id, 1)), _customer);

        Assert.Equal(OrderStatus.Placed, result.Status);
        Assert.Equal(440m, result.TotalAmount);   // 2 x 200 + 1 x 40
        Assert.Equal(1, await _test.Db.Orders.CountAsync());
        Assert.Equal(1, await _test.Db.OrderStatusLogs.CountAsync(l => l.OrderId == result.Id));
    }

    [Fact]
    public async Task Placing_an_order_pushes_one_realtime_event()
    {
        var result = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_coffee.Id, 1)), _customer);

        _notifier.Verify(n => n.OrderChangedAsync(It.Is<OrderChange>(c =>
            c.OrderId == result.Id &&
            c.CustomerId == _customer &&
            c.RestaurantId == _kadai.Id &&
            c.Status == OrderStatus.Placed)), Times.Once);
    }

    [Fact]
    public async Task Same_item_on_two_lines_is_merged_into_one_order_item()
    {
        var result = await _sut.PlaceOrderAsync(
            Request(_kadai.Id, (_coffee.Id, 1), (_coffee.Id, 2)), _customer);

        var item = Assert.Single(result.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(120m, result.TotalAmount);
    }

    [Fact]
    public async Task A_failed_realtime_push_does_not_fail_the_order()
    {
        _notifier.Setup(n => n.OrderChangedAsync(It.IsAny<OrderChange>()))
                 .ThrowsAsync(new InvalidOperationException("hub is down"));

        var result = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_coffee.Id, 1)), _customer);

        Assert.NotNull(result);
        Assert.Equal(1, await _test.Db.Orders.CountAsync());
    }

    [Fact]
    public async Task Closed_restaurant_is_rejected_and_nothing_is_saved()
    {
        var teaId = _closed.MenuItems.First().Id;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_closed.Id, (teaId, 1)), _customer));

        Assert.Equal(0, await _test.Db.Orders.CountAsync());
        _notifier.Verify(n => n.OrderChangedAsync(It.IsAny<OrderChange>()), Times.Never);
    }

    [Fact]
    public async Task Unavailable_item_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_kadai.Id, (_soldOut.Id, 1)), _customer));
    }

    [Fact]
    public async Task Item_from_another_restaurant_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_kadai.Id, (_otherItem.Id, 1)), _customer));
    }

    [Fact]
    public async Task Empty_order_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_kadai.Id), _customer));
    }

    [Fact]
    public async Task Zero_quantity_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_kadai.Id, (_coffee.Id, 0)), _customer));
    }

    [Fact]
    public async Task Unknown_restaurant_gives_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.PlaceOrderAsync(Request(9999, (_coffee.Id, 1)), _customer));
    }
    // ---------------- Stock ----------------

    private Task SetStockAsync(MenuItem item, int? stock) =>
        _test.Db.MenuItems.Where(m => m.Id == item.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Stock, stock));

    // projection always reads the database, not the tracked entity
    private Task<int?> StockOfAsync(MenuItem item) =>
        _test.Db.MenuItems.AsNoTracking().Where(m => m.Id == item.Id).Select(m => m.Stock).SingleAsync();

    [Fact]
    public async Task Stock_is_reduced_when_an_order_is_placed()
    {
        await SetStockAsync(_biryani, 12);

        await _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 9)), _customer);

        Assert.Equal(3, await StockOfAsync(_biryani));
    }

    [Fact]
    public async Task Order_is_rejected_when_stock_is_not_enough_and_nothing_changes()
    {
        await SetStockAsync(_biryani, 3);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 5)), _customer));

        Assert.Contains("Only 3 left", ex.Message);
        Assert.Equal(0, await _test.Db.Orders.CountAsync());
        Assert.Equal(3, await StockOfAsync(_biryani));
        _notifier.Verify(n => n.OrderChangedAsync(It.IsAny<OrderChange>()), Times.Never);
    }

    [Fact]
    public async Task Sold_out_item_is_rejected()
    {
        await SetStockAsync(_biryani, 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 1)), _customer));

        Assert.Contains("sold out", ex.Message);
    }

    [Fact]
    public async Task Items_without_a_stock_limit_are_never_blocked()
    {
        // _coffee.Stock is null = unlimited
        var result = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_coffee.Id, 1000)), _customer);

        Assert.Equal(40_000m, result.TotalAmount);
        Assert.Null(await StockOfAsync(_coffee));
    }

    [Fact]
    public async Task Failure_on_a_later_item_gives_back_stock_taken_for_earlier_items()
    {
        // biryani is reserved first (lower id), then coffee fails -> biryani must be rolled back
        await SetStockAsync(_biryani, 1);
        await SetStockAsync(_coffee, 5);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PlaceOrderAsync(
            Request(_kadai.Id, (_biryani.Id, 1), (_coffee.Id, 10)), _customer));

        Assert.Equal(1, await StockOfAsync(_biryani));   // transaction rolled back
        Assert.Equal(5, await StockOfAsync(_coffee));
        Assert.Equal(0, await _test.Db.Orders.CountAsync());
    }
    [Fact]
    public async Task Cancelling_an_order_gives_the_stock_back()
    {
        await SetStockAsync(_biryani, 12);
        var order = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 9)), _customer);
        Assert.Equal(3, await StockOfAsync(_biryani));

        await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(order.Id, OrderStatus.Cancelled, null), _customer, Roles.Customer);

        Assert.Equal(12, await StockOfAsync(_biryani));
    }

    [Fact]
    public async Task Rejecting_an_order_gives_the_stock_back()
    {
        await SetStockAsync(_biryani, 12);
        var order = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 9)), _customer);

        await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(order.Id, OrderStatus.Rejected, "no stock"), _owner, Roles.RestaurantOwner);

        Assert.Equal(12, await StockOfAsync(_biryani));
    }

    [Fact]
    public async Task Accepting_an_order_does_not_give_stock_back()
    {
        await SetStockAsync(_biryani, 12);
        var order = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 9)), _customer);

        await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(order.Id, OrderStatus.Accepted, null), _owner, Roles.RestaurantOwner);

        Assert.Equal(3, await StockOfAsync(_biryani));
    }

    [Fact]
    public async Task Cancelling_an_order_with_unlimited_items_keeps_them_unlimited()
    {
        var order = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_coffee.Id, 2)), _customer);

        await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(order.Id, OrderStatus.Cancelled, null), _customer, Roles.Customer);

        Assert.Null(await StockOfAsync(_coffee));
    }

    [Fact]
    public async Task Placing_an_order_pushes_the_new_stock_level_to_the_menu()
    {
        await SetStockAsync(_biryani, 12);

        await _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 9)), _customer);

        _notifier.Verify(n => n.StockChangedAsync(_kadai.Id,
            It.Is<IReadOnlyList<StockLevel>>(l => l.Any(x => x.MenuItemId == _biryani.Id && x.Stock == 3))),
            Times.Once);
    }

    [Fact]
    public async Task Cancelling_an_order_pushes_the_restored_stock_level()
    {
        await SetStockAsync(_biryani, 12);
        var order = await _sut.PlaceOrderAsync(Request(_kadai.Id, (_biryani.Id, 9)), _customer);
        _notifier.Invocations.Clear();

        await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(order.Id, OrderStatus.Cancelled, null), _customer, Roles.Customer);

        _notifier.Verify(n => n.StockChangedAsync(_kadai.Id,
            It.Is<IReadOnlyList<StockLevel>>(l => l.Any(x => x.MenuItemId == _biryani.Id && x.Stock == 12))),
            Times.Once);
    }

    // ---------------- Coupons ----------------

    // 20% off, minimum order 100, valid from yesterday to tomorrow (fake clock = 15 Oct 2026, 12:00 UTC)
    private async Task<Coupon> AddCouponAsync(Action<Coupon>? tweak = null)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var c = new Coupon
        {
            Code = "SAVE20",
            Type = DiscountType.Percent,
            Value = 20,
            MinOrderAmount = 100,
            ValidFromUtc = now.AddDays(-1),
            ValidToUtc = now.AddDays(1)
        };
        tweak?.Invoke(c);
        _test.Db.Coupons.Add(c);
        await _test.Db.SaveChangesAsync();
        return c;
    }

    private CreateOrderDto Biryani(int qty, string? coupon) =>
        Request(_kadai.Id, (_biryani.Id, qty)) with { CouponCode = coupon };

    private Task<int> UsedCountAsync(Coupon c) =>
        _test.Db.Coupons.AsNoTracking().Where(x => x.Id == c.Id).Select(x => x.UsedCount).SingleAsync();

    [Fact]
    public async Task Coupon_reduces_the_total_and_is_recorded()
    {
        var coupon = await AddCouponAsync();

        var result = await _sut.PlaceOrderAsync(Biryani(2, "save20"), _customer);   // lower case on purpose

        Assert.Equal(80m, result.DiscountAmount);
        Assert.Equal(320m, result.TotalAmount);          // 400 - 20%
        Assert.Equal("SAVE20", result.CouponCode);
        Assert.Equal(1, await _test.Db.CouponRedemptions.CountAsync(r => r.CouponId == coupon.Id));
        Assert.Equal(1, await UsedCountAsync(coupon));
    }

    [Fact]
    public async Task Percent_discount_is_capped_by_the_maximum()
    {
        await AddCouponAsync(c => c.MaxDiscount = 50);

        var result = await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        Assert.Equal(50m, result.DiscountAmount);
        Assert.Equal(350m, result.TotalAmount);
    }

    [Fact]
    public async Task Flat_coupon_takes_a_fixed_amount_off()
    {
        await AddCouponAsync(c => { c.Type = DiscountType.Flat; c.Value = 30; });

        var result = await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        Assert.Equal(370m, result.TotalAmount);
    }

    [Fact]
    public async Task A_customer_cannot_use_the_same_coupon_twice()
    {
        var coupon = await AddCouponAsync();
        await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer));

        Assert.Contains("already used", ex.Message);
        Assert.Equal(1, await _test.Db.Orders.CountAsync());
        Assert.Equal(1, await UsedCountAsync(coupon));
    }

    [Fact]
    public async Task The_database_itself_refuses_a_second_redemption_by_the_same_customer()
    {
        // skips the friendly pre-check on purpose: this is what stops two simultaneous requests
        var coupon = await AddCouponAsync();
        var applied = new CouponApplication(coupon.Id, coupon.Code, 10m);

        await _redeemer.RedeemAsync(applied, _customer, 1001);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _redeemer.RedeemAsync(applied, _customer, 1002));

        Assert.Contains("already used", ex.Message);
    }

    [Fact]
    public async Task Another_customer_can_still_use_a_coupon_that_one_customer_used()
    {
        await AddCouponAsync();
        await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        var result = await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), Guid.NewGuid());

        Assert.Equal(80m, result.DiscountAmount);
    }

    [Fact]
    public async Task Coupon_assigned_to_someone_else_is_rejected()
    {
        await AddCouponAsync(c => c.AssignedUserId = Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer));

        Assert.Contains("not valid", ex.Message);
    }

    [Fact]
    public async Task Assigned_coupon_works_for_the_assigned_customer()
    {
        await AddCouponAsync(c => c.AssignedUserId = _customer);

        var result = await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        Assert.Equal(80m, result.DiscountAmount);
    }

    [Fact]
    public async Task Expired_coupon_is_rejected()
    {
        await AddCouponAsync();
        _clock.Advance(TimeSpan.FromDays(2));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer));

        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public async Task Coupon_that_has_not_started_yet_is_rejected()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        await AddCouponAsync(c => { c.ValidFromUtc = now.AddHours(1); c.ValidToUtc = now.AddDays(2); });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer));

        Assert.Contains("not active yet", ex.Message);
    }

    [Fact]
    public async Task Order_below_the_minimum_amount_is_rejected()
    {
        await AddCouponAsync();   // minimum 100, one coffee is 40

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PlaceOrderAsync(
            Request(_kadai.Id, (_coffee.Id, 1)) with { CouponCode = "SAVE20" }, _customer));

        Assert.Contains("Minimum order", ex.Message);
    }

    [Fact]
    public async Task Coupon_with_no_uses_left_is_rejected()
    {
        await AddCouponAsync(c => c.UsageLimit = 1);
        await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), Guid.NewGuid()));

        Assert.Contains("fully used", ex.Message);
    }

    [Fact]
    public async Task Disabled_or_unknown_codes_give_the_same_message()
    {
        await AddCouponAsync(c => c.IsActive = false);

        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer));
        var unknown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "NOSUCHCODE"), _customer));

        Assert.Equal(disabled.Message, unknown.Message);
    }

    [Fact]
    public async Task A_failed_order_does_not_use_up_the_coupon()
    {
        var coupon = await AddCouponAsync();
        await SetStockAsync(_biryani, 1);   // order asks for 2 -> stock fails

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer));

        Assert.Equal(0, await UsedCountAsync(coupon));
        Assert.Equal(0, await _test.Db.CouponRedemptions.CountAsync());
    }

    [Fact]
    public async Task Cancelling_an_order_gives_the_coupon_back()
    {
        var coupon = await AddCouponAsync();
        var order = await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);

        await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(order.Id, OrderStatus.Cancelled, null), _customer, Roles.Customer);

        Assert.Equal(0, await UsedCountAsync(coupon));
        Assert.Equal(0, await _test.Db.CouponRedemptions.CountAsync());

        // and the customer can use it again
        var again = await _sut.PlaceOrderAsync(Biryani(2, "SAVE20"), _customer);
        Assert.Equal(80m, again.DiscountAmount);
    }

    // ---------------- UpdateStatusAsync ----------------

    private async Task<int> PlaceOrderAsync() =>
        (await _sut.PlaceOrderAsync(Request(_kadai.Id, (_coffee.Id, 1)), _customer)).Id;

    [Fact]
    public async Task Owner_accepting_an_order_changes_status_logs_it_and_pushes()
    {
        var id = await PlaceOrderAsync();
        _notifier.Invocations.Clear();   // forget the push from placing the order

        var result = await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(id, OrderStatus.Accepted, "ok"), _owner, Roles.RestaurantOwner);

        Assert.Equal(OrderStatus.Accepted, result.Status);
        Assert.Equal(2, await _test.Db.OrderStatusLogs.CountAsync(l => l.OrderId == id));   // placed + accepted
        _notifier.Verify(n => n.OrderChangedAsync(It.Is<OrderChange>(c =>
            c.OrderId == id && c.Status == OrderStatus.Accepted)), Times.Once);
    }

    [Fact]
    public async Task Invalid_transition_is_rejected_and_status_stays_the_same()
    {
        var id = await PlaceOrderAsync();
        _notifier.Invocations.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(id, OrderStatus.Delivered, null), _owner, Roles.RestaurantOwner));

        Assert.Equal(OrderStatus.Placed, (await _test.Db.Orders.FindAsync(id))!.Status);
        _notifier.Verify(n => n.OrderChangedAsync(It.IsAny<OrderChange>()), Times.Never);
    }

    [Fact]
    public async Task Another_restaurants_owner_gets_forbidden()
    {
        var id = await PlaceOrderAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(id, OrderStatus.Accepted, null), Guid.NewGuid(), Roles.RestaurantOwner));
    }

    [Fact]
    public async Task Customer_can_cancel_own_order_while_it_is_placed()
    {
        var id = await PlaceOrderAsync();

        var result = await _sut.UpdateStatusAsync(
            new UpdateOrderStatusDto(id, OrderStatus.Cancelled, null), _customer, Roles.Customer);

        Assert.Equal(OrderStatus.Cancelled, result.Status);
    }
}