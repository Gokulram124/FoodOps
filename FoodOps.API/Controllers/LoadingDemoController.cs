using FoodOps.Application.Common;
using FoodOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.API.Controllers;

// DEMO ONLY: works in Development, returns 404 elsewhere
[ApiController]
[Route("api/demo/loading")]
[Authorize(Roles = Roles.Admin)]
public class LoadingDemoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public LoadingDemoController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // N+1: same thing lazy loading does silently
    [HttpGet("explicit-n-plus-one")]
    public async Task<IActionResult> ExplicitNPlusOne()
    {
        if (!_env.IsDevelopment()) return NotFound();

        var orders = await _db.Orders.OrderByDescending(o => o.Id).Take(5).ToListAsync();   // 1 query
        foreach (var o in orders)
        {
            await _db.Entry(o).Reference(x => x.Restaurant).LoadAsync();   // +1 per order
            await _db.Entry(o).Collection(x => x.Items).LoadAsync();       // +1 per order
        }
        return Ok(orders.Select(o => new { o.Id, Restaurant = o.Restaurant!.Name, Items = o.Items.Count }));
    }

    // Eager loading: Include (one big JOIN query), ?split=true -> split queries
    [HttpGet("eager")]
    public async Task<IActionResult> Eager([FromQuery] bool split = false)
    {
        if (!_env.IsDevelopment()) return NotFound();

        IQueryable<FoodOps.Domain.Entities.Order> q = _db.Orders.AsNoTracking()
            .Include(o => o.Restaurant)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .OrderByDescending(o => o.Id).Take(5);
        if (split) q = q.AsSplitQuery();

        var orders = await q.ToListAsync();
        return Ok(orders.Select(o => new
        {
            o.Id,
            Restaurant = o.Restaurant!.Name,
            Items = o.Items.Select(i => i.MenuItem!.Name)
        }));
    }

    // Projection: only the columns we need (what ProjectToDto does in OrderService)
    [HttpGet("projection")]
    public async Task<IActionResult> Projection()
    {
        if (!_env.IsDevelopment()) return NotFound();

        var data = await _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.Id).Take(5)
            .Select(o => new
            {
                o.Id,
                Restaurant = o.Restaurant!.Name,
                Items = o.Items.Select(i => i.MenuItem!.Name).ToList()
            })
            .ToListAsync();
        return Ok(data);
    }
}