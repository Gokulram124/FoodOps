using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodOps.Application.DTOs.Menu;

public record MenuItemDto(int Id, int RestaurantId, string Name, decimal Price, string Category, bool IsAvailable, string? ImageUrl = null);
public record CreateMenuItemDto(int RestaurantId, string Name, decimal Price, string Category);
public record UpdateMenuItemDto(string Name, decimal Price, string Category, bool IsAvailable);