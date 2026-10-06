using Asp.Versioning;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Restaurants;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[ApiVersion("2.0")]
[Route("api/restaurants")]
[Route("api/v{version:apiVersion}/restaurants")]
public class RestaurantsV2Controller : ControllerBase
{
    private readonly IRestaurantService _svc;
    public RestaurantsV2Controller(IRestaurantService svc) => _svc = svc;

    // v2 breaking change: "isOpen" (bool) became "status" (string)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RestaurantQuery query)
    {
        var result = await _svc.GetAllAsync(query);
        var items = result.Items
            .Select(r => new RestaurantV2Dto(r.Id, r.Name, r.City, r.Rating, r.IsOpen ? "Open" : "Closed"))
            .ToList();

        return Ok(new PagedResult<RestaurantV2Dto>(items, result.TotalCount, result.Page, result.PageSize));
    }
}