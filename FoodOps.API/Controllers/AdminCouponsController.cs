using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminCouponsController : ControllerBase
{
    private readonly ICouponService _svc;
    public AdminCouponsController(ICouponService svc) => _svc = svc;

    [HttpGet("coupons")]
    public async Task<ActionResult<IReadOnlyList<CouponDto>>> List()
        => Ok(await _svc.GetAllAsync());

    [HttpPost("coupons")]
    public async Task<IActionResult> Create(CreateCouponDto dto)
        => StatusCode(StatusCodes.Status201Created, await _svc.CreateAsync(dto));

    [HttpPut("coupons/{id:int}/active")]
    public async Task<ActionResult<CouponDto>> SetActive(int id, SetCouponActiveDto dto)
        => Ok(await _svc.SetActiveAsync(id, dto.IsActive));

    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<CustomerOptionDto>>> Customers()
        => Ok(await _svc.GetCustomersAsync());
}