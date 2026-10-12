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

        _sut = new OrderService(
            new UnitOfWork(_test.Db),
            new IStatusTransitionRule[] { new CustomerCancelRule(), new RestaurantFlowRule() },
            Mock.Of<IOrderNotificationFactory>(),
            _notifier.Object,
            NullLogger<OrderService>.Instance);
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