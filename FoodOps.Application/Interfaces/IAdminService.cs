using FoodOps.Application.DTOs.Admin;

namespace FoodOps.Application.Interfaces;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(int tzOffsetMinutes);
}