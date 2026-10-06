using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Menu;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _svc;
    public MenuController(IMenuService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetByRestaurant([FromQuery] int restaurantId)
        => Ok(await _svc.GetByRestaurantAsync(restaurantId));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateMenuItemDto dto)
        => Ok(await _svc.CreateAsync(dto, User.GetUserId(), User.IsAdmin()));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateMenuItemDto dto)
        => Ok(await _svc.UpdateAsync(id, dto, User.GetUserId(), User.IsAdmin()));

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }

    [Authorize(Roles = $"{Roles.RestaurantOwner},{Roles.Admin}")]
    [HttpPost("{id:int}/image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await _svc.UploadImageAsync(id, stream, file.FileName, file.Length, User.GetUserId(), User.IsAdmin()));
    }
}