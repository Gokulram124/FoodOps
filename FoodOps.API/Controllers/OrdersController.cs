using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Orders;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _svc;
    public OrdersController(IOrderService svc) => _svc = svc;

    [Authorize(Roles = Roles.Customer)]
    [HttpPost]
    public async Task<IActionResult> Place(CreateOrderDto dto)
        => Ok(await _svc.PlaceOrderAsync(dto, User.GetUserId()));

    [Authorize(Roles = Roles.Customer)]
    [HttpGet("my")]
    public async Task<IActionResult> My()
        => Ok(await _svc.GetMyOrdersAsync(User.GetUserId()));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpGet("restaurant")]
    public async Task<IActionResult> RestaurantOrders([FromQuery] OrderStatus? status)
        => Ok(await _svc.GetRestaurantOrdersAsync(User.GetUserId(), User.IsAdmin(), status));

    [Authorize(Roles = $"{Roles.Customer},{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus(UpdateOrderStatusDto dto)
        => Ok(await _svc.UpdateStatusAsync(dto, User.GetUserId(), User.GetRole()));

    [HttpGet("{id:int}/timeline")]
    public async Task<IActionResult> Timeline(int id)
        => Ok(await _svc.GetTimelineAsync(id, User.GetUserId(), User.GetRole()));
}