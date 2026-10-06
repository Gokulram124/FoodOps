using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodOps.Application.DTOs.Restaurants;

public record RestaurantDto(int Id, string Name, string City, decimal Rating, bool IsOpen);
public record CreateRestaurantDto(string Name, string City);
public record UpdateRestaurantDto(string Name, string City, bool IsOpen);
public record RestaurantV2Dto(int Id, string Name, string City, decimal Rating, string Status);
