using FoodOps.Application.Operations;
using Microsoft.Extensions.Options;

namespace FoodOps.API.Workers;

public class OpsScanWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOpsReportStore _store;
    private readonly OpsOptions _options;
    private readonly ILogger<OpsScanWorker> _logger;

    // What we already logged, so the same alert is not logged every 30 seconds
    private HashSet<string> _known = new();

    public OpsScanWorker(
        IServiceScopeFactory scopeFactory,
        IOpsReportStore store,
        IOptions<OpsOptions> options,
        ILogger<OpsScanWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.ScanIntervalSeconds));
        _logger.LogInformation("Ops scanner started, scanning every {Seconds}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                await ScanOnceAsync();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // application is shutting down
        }
    }

    private async Task ScanOnceAsync()
    {
        try
        {
            // The worker is a singleton, DbContext is scoped: create a scope for every scan
            using var scope = _scopeFactory.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<IOperationsMonitor>();
            var report = await monitor.ScanAsync();
            _store.Save(report);

            var current = new HashSet<string>();

            foreach (var d in report.Delays)
            {
                var key = $"delay:{d.OrderId}:{d.Level}";
                current.Add(key);
                if (!_known.Contains(key))
                    _logger.LogWarning("Order {OrderId} at {Restaurant} is {Level}: {Reasons}",
                        d.OrderId, d.RestaurantName, d.Level, string.Join("; ", d.Reasons));
            }

            foreach (var a in report.Alerts)
            {
                var key = $"alert:{a.Type}:{a.Subject}:{a.Severity}";
                current.Add(key);
                if (!_known.Contains(key))
                    _logger.LogWarning("{Type} {Severity} - {Subject}: {Message}",
                        a.Type, a.Severity, a.Subject, a.Message);
            }

            _known = current;
        }
        catch (Exception ex)
        {
            // One failed scan must not kill the worker
            _logger.LogError(ex, "Ops scan failed");
        }
    }
}