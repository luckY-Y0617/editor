using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Northstar.Infrastructure.Security;

public sealed class PermissionExpiryNotificationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PermissionExpiryNotificationOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PermissionExpiryNotificationHostedService> _logger;

    public PermissionExpiryNotificationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<PermissionExpiryNotificationOptions> options,
        IHostEnvironment environment,
        ILogger<PermissionExpiryNotificationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || _environment.IsEnvironment("Testing"))
        {
            return;
        }

        await RunScanAsync(stoppingToken);
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.ScanIntervalMinutes));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScanAsync(stoppingToken);
        }
    }

    private async Task RunScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<PermissionExpiryNotificationProcessor>();
            await processor.RunOnceAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Permission expiry notification scan failed.");
        }
    }
}
