using FoodOps.API.Controllers;
using FoodOps.Application.Analytics;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FoodOps.Tests.Analytics;

public class AnalyticsControllerTests
{
    private readonly Mock<IAnalyticsRepository> _repo = new();
    private readonly AnalyticsController _sut;

    public AnalyticsControllerTests() => _sut = new AnalyticsController(_repo.Object);

    [Theory]
    [InlineData(1000, 50)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(10, 10)]
    public async Task TopItems_clamps_take_between_1_and_50(int requested, int expected)
    {
        _repo.Setup(r => r.GetTopItemsAsync(1, expected, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<TopItemDto>());

        var result = await _sut.TopItems(1, requested, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _repo.Verify(r => r.GetTopItemsAsync(1, expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("year")]
    [InlineData("")]
    [InlineData("day; DROP TABLE Orders")]
    public async Task RevenueTrend_rejects_unknown_granularity(string granularity)
    {
        var result = await _sut.RevenueTrend(null, null, granularity, 330, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _repo.VerifyNoOtherCalls();   // the database is never touched
    }

    [Fact]
    public async Task RevenueTrend_accepts_granularity_in_any_case_and_passes_it_lowercase()
    {
        _repo.Setup(r => r.GetRevenueTrendAsync(
                  It.IsAny<DateTime>(), It.IsAny<DateTime>(), "week", 330, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<RevenueTrendPointDto>());

        var result = await _sut.RevenueTrend(null, null, "WEEK", 330, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _repo.Verify(r => r.GetRevenueTrendAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), "week", 330, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevenueTrend_rejects_from_not_before_to()
    {
        var t = new DateTime(2026, 10, 10);
        var result = await _sut.RevenueTrend(t, t, "day", 0, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Dashboard_rejects_from_not_before_to()
    {
        var t = new DateTime(2026, 10, 10);
        var result = await _sut.Dashboard(t, t, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Leaderboard_rejects_from_not_before_to()
    {
        var t = new DateTime(2026, 10, 10);
        var result = await _sut.Leaderboard(t, t, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RestaurantPerformance_rejects_from_not_before_to()
    {
        var t = new DateTime(2026, 10, 10);
        var result = await _sut.RestaurantPerformance(t, t, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task OrderReport_returns_404_when_order_does_not_exist()
    {
        _repo.Setup(r => r.GetOrderReportAsync(999, It.IsAny<CancellationToken>()))
             .ReturnsAsync((OrderReportDto?)null);

        var result = await _sut.OrderReport(999, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task OrderReport_returns_the_report_when_it_exists()
    {
        var report = new OrderReportDto { Id = 1010 };
        _repo.Setup(r => r.GetOrderReportAsync(1010, It.IsAny<CancellationToken>()))
             .ReturnsAsync(report);

        var result = await _sut.OrderReport(1010, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(report, ok.Value);
    }
}