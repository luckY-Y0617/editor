using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Northstar.Application.Files;

namespace Northstar.Infrastructure.Files;

public sealed class FileOutboxHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FileOutboxOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FileOutboxHostedService> _logger;

    public FileOutboxHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<FileOutboxOptions> options,
        IHostEnvironment environment,
        ILogger<FileOutboxHostedService> logger)
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

        await ProcessOnceAsync(stoppingToken);
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.ScanIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IFileOutboxProcessor>();
            await processor.ProcessDueAsync(
                batchSize: Math.Clamp(_options.BatchSize, 1, 100),
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "File outbox processing failed.");
        }
    }
}
