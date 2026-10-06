using Asp.Versioning;
using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Restaurants;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodOps.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/restaurants")]
[Route("api/v{version:apiVersion}/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _svc;
    private readonly IOutputCacheStore _cache;

    public RestaurantsController(IRestaurantService svc, IOutputCacheStore cache)
    {
        _svc = svc;
        _cache = cache;
    }

    [HttpGet]
    [OutputCache(PolicyName = "RestaurantList")]
    public async Task<IActionResult> GetAll([FromQuery] RestaurantQuery query)
    => Ok(await _svc.GetAllAsync(query));

    [HttpGet("keyset")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetKeyset([FromQuery] string? cursor, [FromQuery] int pageSize = 10)
    => Ok(await _svc.GetKeysetAsync(cursor, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => Ok(await _svc.GetByIdAsync(id));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRestaurantDto dto)
    {
        var created = await _svc.CreateAsync(dto, User.GetUserId());
        await _cache.EvictByTagAsync("restaurants", HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRestaurantDto dto)
    {
        var updated = await _svc.UpdateAsync(id, dto, User.GetUserId(), User.IsAdmin());
        await _cache.EvictByTagAsync("restaurants", HttpContext.RequestAborted);
        return Ok(updated);
    }

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, User.GetUserId(), User.IsAdmin());
        await _cache.EvictByTagAsync("restaurants", HttpContext.RequestAborted);
        return NoContent();
    }
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities() => Ok(await _svc.GetCitiesAsync());
}