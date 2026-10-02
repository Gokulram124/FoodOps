using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Delivery;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.DeliveryPartner)]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _svc;
    public DeliveryController(IDeliveryService svc) => _svc = svc;

    [HttpPut("availability")]
    public async Task<IActionResult> Availability(SetAvailabilityDto dto)
        => Ok(await _svc.SetAvailabilityAsync(User.GetUserId(), User.GetFullName(), dto));

    [HttpGet("available")]
    public async Task<IActionResult> Available() => Ok(await _svc.GetAvailableOrdersAsync());

    [HttpPost("assign/{orderId:int}")]
    public async Task<IActionResult> Assign(int orderId)
        => Ok(await _svc.AssignAsync(orderId, User.GetUserId()));

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus(UpdateDeliveryStatusDto dto)
        => Ok(await _svc.UpdateStatusAsync(dto, User.GetUserId()));

    [HttpGet("my")]
    public async Task<IActionResult> My() => Ok(await _svc.GetMyDeliveriesAsync(User.GetUserId()));
}