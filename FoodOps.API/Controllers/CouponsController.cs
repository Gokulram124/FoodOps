using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Orders;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Customer)]
[Route("api/coupons")]
public class CouponsController : ControllerBase
{
    private readonly ICouponRedeemer _coupons;
    public CouponsController(ICouponRedeemer coupons) => _coupons = coupons;

    // "Apply" button in the cart: shows the discount, uses nothing up
    [HttpPost("validate")]
    public async Task<ActionResult<CouponQuote>> Validate(ValidateCouponDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");
        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Quantity must be greater than zero.");

        var items = dto.Items.GroupBy(i => i.MenuItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        return Ok(await _coupons.QuoteAsync(dto.Code, User.GetUserId(), dto.RestaurantId, items));
    }
}