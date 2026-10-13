using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Delivery;
using FoodOps.Application.Notifications;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Services;
using FoodOps.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FoodOps.Tests.Services;

public class DeliveryServiceTests : IDisposable
{
    private readonly TestDb _test = new();
    private readonly Mock<IOrderNotifier> _notifier = new();
    private readonly DeliveryService _sut;

    private readonly Guid _customer = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly Guid _userOffline = Guid.NewGuid();

    private readonly Order _order;
    private readonly Rider _riderA;
    private readonly Rider _riderB;

    public DeliveryServiceTests()
    {
        var restaurant = new Restaurant { Name = "Test Kadai", City = "Chennai", OwnerId = Guid.NewGuid() };
        _test.Db.Restaurants.Add(restaurant);
        _test.Db.SaveChanges();

        _order = new Order
        {
            CustomerId = _customer,
            RestaurantId = restaurant.Id,
            Status = OrderStatus.ReadyForPickup,
            TotalAmount = 100
        };
        _riderA = new Rider { UserId = _userA, Name = "Rider A", Phone = "111", IsOnline = true };
        _riderB = new Rider { UserId = _userB, Name = "Rider B", Phone = "222", IsOnline = true };
        var offline = new Rider { UserId = _userOffline, Name = "Rider C", Phone = "333", IsOnline = false };

        _test.Db.Orders.Add(_order);
        _test.Db.Riders.AddRange(_riderA, _riderB, offline);
        _test.Db.SaveChanges();
        _test.Db.ChangeTracker.Clear();   // behave like a fresh request

        _sut = new DeliveryService(_test.Db, _notifier.Object, NullLogger<DeliveryService>.Instance);
    }

    public void Dispose() => _test.Dispose();

    [Fact]
    public async Task Online_rider_can_take_a_ready_order_and_a_push_is_sent()
    {
        var result = await _sut.AssignAsync(_order.Id, _userA);

        Assert.Equal(_riderA.Id, result.RiderId);
        Assert.Equal(1, await _test.Db.Deliveries.CountAsync(d => d.OrderId == _order.Id));
        _notifier.Verify(n => n.OrderChangedAsync(It.Is<OrderChange>(c =>
            c.OrderId == _order.Id && c.CustomerId == _customer)), Times.Once);
    }

    [Fact]
    public async Task Second_rider_loses_the_race_for_the_same_order()
    {
        await _sut.AssignAsync(_order.Id, _userA);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AssignAsync(_order.Id, _userB));
    }

    [Fact]
    public async Task Offline_rider_cannot_accept_orders()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AssignAsync(_order.Id, _userOffline));
    }

    [Fact]
    public async Task Rider_walks_the_order_through_pickup_and_delivery()
    {
        await _sut.AssignAsync(_order.Id, _userA);
        // ExecuteUpdate bypasses the change tracker; a real request gets a fresh DbContext, so do the same here
        _test.Db.ChangeTracker.Clear();

        var picked = await _sut.UpdateStatusAsync(new UpdateDeliveryStatusDto(_order.Id, OrderStatus.PickedUp), _userA);
        var delivered = await _sut.UpdateStatusAsync(new UpdateDeliveryStatusDto(_order.Id, OrderStatus.Delivered), _userA);

        Assert.Equal(OrderStatus.PickedUp, picked.Status);
        Assert.Equal(OrderStatus.Delivered, delivered.Status);

        var delivery = await _test.Db.Deliveries.AsNoTracking().SingleAsync();
        Assert.NotNull(delivery.PickupTime);
        Assert.NotNull(delivery.DeliveredTime);

        _notifier.Verify(n => n.OrderChangedAsync(It.Is<OrderChange>(c => c.Status == OrderStatus.PickedUp)), Times.Once);
        _notifier.Verify(n => n.OrderChangedAsync(It.Is<OrderChange>(c => c.Status == OrderStatus.Delivered)), Times.Once);
    }

    [Fact]
    public async Task Rider_cannot_skip_the_pickup_step()
    {
        await _sut.AssignAsync(_order.Id, _userA);
        _test.Db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateStatusAsync(new UpdateDeliveryStatusDto(_order.Id, OrderStatus.Delivered), _userA));
    }

    [Fact]
    public async Task Another_rider_cannot_update_an_order_assigned_to_someone_else()
    {
        await _sut.AssignAsync(_order.Id, _userA);
        _test.Db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateStatusAsync(new UpdateDeliveryStatusDto(_order.Id, OrderStatus.PickedUp), _userB));
    }
}