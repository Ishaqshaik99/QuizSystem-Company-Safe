using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuizSystem.Core.Interfaces;

namespace QuizSystem.Infrastructure.Services;

public class ExpiredAttemptAutoSubmitHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredAttemptAutoSubmitHostedService> _logger;

    public ExpiredAttemptAutoSubmitHostedService(IServiceProvider serviceProvider, ILogger<ExpiredAttemptAutoSubmitHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var attemptService = scope.ServiceProvider.GetRequiredService<IAttemptService>();
                var submitted = await attemptService.AutoSubmitExpiredAsync(stoppingToken);

                if (submitted > 0)
                {
                    _logger.LogInformation("Auto-submitted {Count} expired attempts.", submitted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed while auto-submitting expired attempts.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
