using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeQuest.Infrastructure.Services;

namespace TimeQuest.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs TicketValidationService every 15 minutes.
/// Implements F6.1 offline fallback: entries with TicketValidationStatus=Pending are retried.
/// </summary>
public class TicketValidationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketValidationBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public TicketValidationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TicketValidationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TicketValidationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("TicketValidationBackgroundService: running validation cycle.");

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TicketValidationService>();
                await service.ValidatePendingTicketsAsync(stoppingToken);

                _logger.LogInformation("TicketValidationBackgroundService: validation cycle complete.");
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TicketValidationBackgroundService: unhandled error in validation cycle.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("TicketValidationBackgroundService stopped.");
    }
}
