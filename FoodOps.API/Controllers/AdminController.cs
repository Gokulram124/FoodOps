using DocumentFormat.OpenXml.Bibliography;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.DTOs.Restaurants;
using FoodOps.Application.Interfaces;
using FoodOps.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _svc;
    private readonly IOperationsMonitor _monitor;
    private readonly IOpsReportStore _store;
    private readonly IOutputCacheStore _cache;
    private readonly IReportExporter _exporter;


    public AdminController(IAdminService svc, IOperationsMonitor monitor, IOpsReportStore store, IOutputCacheStore cache, IReportExporter exporter)
    {
        _svc = svc;
        _monitor = monitor;
        _store = store;
        _cache = cache;
        _exporter = exporter;
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<OpsReport>> Alerts() => Ok(await _monitor.ScanAsync());

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats([FromQuery] int tzOffsetMinutes = 0)
        => Ok(await _svc.GetStatsAsync(tzOffsetMinutes));

    [HttpGet("ops-report")]
    public ActionResult<OpsReport> LatestReport()
    => _store.Latest is { } report ? Ok(report) : NoContent();

    [HttpGet("owners")]
    public async Task<ActionResult<IReadOnlyList<OwnerSummaryDto>>> Owners()
    => Ok(await _svc.GetOwnersAsync());

    [HttpPost("owners")]
    public async Task<ActionResult<OwnerSummaryDto>> CreateOwner(CreateOwnerDto dto)
        => StatusCode(StatusCodes.Status201Created, await _svc.CreateOwnerAsync(dto));

    [HttpGet("restaurants")]
    public async Task<ActionResult<IReadOnlyList<AdminRestaurantDto>>> Restaurants([FromQuery] int tzOffsetMinutes = 0)
    => Ok(await _svc.GetRestaurantsAsync(tzOffsetMinutes));

    [HttpPost("restaurants")]
    public async Task<IActionResult> CreateRestaurant(AdminCreateRestaurantDto dto)
    {
        var created = await _svc.CreateRestaurantAsync(dto);
        await _cache.EvictByTagAsync("restaurants", HttpContext.RequestAborted);   // customers' cached list must refresh
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("reports/revenue")]
    public async Task<ActionResult<IReadOnlyList<RevenuePointDto>>> Revenue(
    [FromQuery] string period = "day",
    [FromQuery] int? restaurantId = null,
    [FromQuery] Guid? ownerId = null,
    [FromQuery] int tzOffsetMinutes = 0)
    => Ok(await _svc.GetRevenueAsync(period, restaurantId, ownerId, tzOffsetMinutes));
    [HttpGet("reports/revenue/export")]
    public async Task<IActionResult> ExportRevenue(
    [FromQuery] string period = "day",
    [FromQuery] int? restaurantId = null,
    [FromQuery] Guid? ownerId = null,
    [FromQuery] int tzOffsetMinutes = 0)
    {
        var rows = await _svc.GetRevenueAsync(period, restaurantId, ownerId, tzOffsetMinutes);
        var bytes = _exporter.RevenueToExcel(period, rows);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"revenue-{period}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}