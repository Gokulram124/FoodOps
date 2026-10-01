using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Restaurants;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _svc;
    public RestaurantsController(IRestaurantService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? city,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _svc.GetAllAsync(search, city, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => Ok(await _svc.GetByIdAsync(id));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRestaurantDto dto)
    {
        var created = await _svc.CreateAsync(dto, User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRestaurantDto dto)
        => Ok(await _svc.UpdateAsync(id, dto, User.GetUserId(), User.IsAdmin()));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }
}