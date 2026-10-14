using FoodOps.Application.DTOs.Admin;

namespace FoodOps.Application.Interfaces;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(int tzOffsetMinutes);
    Task<OwnerSummaryDto> CreateOwnerAsync(CreateOwnerDto dto);
    Task<IReadOnlyList<OwnerSummaryDto>> GetOwnersAsync();
}