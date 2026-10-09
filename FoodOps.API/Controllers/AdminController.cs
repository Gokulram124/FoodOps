using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;
using FoodOps.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _svc;
    private readonly IOperationsMonitor _monitor;
    private readonly IOpsReportStore _store;

    public AdminController(IAdminService svc, IOperationsMonitor monitor, IOpsReportStore store)
    {
        _svc = svc;
        _monitor = monitor;
        _store = store;
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<OpsReport>> Alerts() => Ok(await _monitor.ScanAsync());

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats([FromQuery] int tzOffsetMinutes = 0)
        => Ok(await _svc.GetStatsAsync(tzOffsetMinutes));

    [HttpGet("ops-report")]
    public ActionResult<OpsReport> LatestReport()
    => _store.Latest is { } report ? Ok(report) : NoContent();
}