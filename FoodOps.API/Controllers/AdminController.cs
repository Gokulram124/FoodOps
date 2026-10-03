using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _svc;
    public AdminController(IAdminService svc) => _svc = svc;

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats([FromQuery] int tzOffsetMinutes = 0)
        => Ok(await _svc.GetStatsAsync(tzOffsetMinutes));
}