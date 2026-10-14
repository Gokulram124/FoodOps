using FoodOps.Application.DTOs.Admin;

namespace FoodOps.Application.Interfaces;

public interface IReportExporter
{
    byte[] RevenueToExcel(string period, IReadOnlyList<RevenuePointDto> rows);
}