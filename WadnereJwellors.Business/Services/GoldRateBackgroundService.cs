using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WadnereJwellors.Business.Services
{
    public class GoldRateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GoldRateBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15); // Update every 15 seconds

        public GoldRateBackgroundService(IServiceProvider serviceProvider, ILogger<GoldRateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Gold & Silver Live Market Continuous Worker Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var goldRateService = scope.ServiceProvider.GetRequiredService<IGoldRateService>();
                        await goldRateService.FetchAndBroadcastLiveRatesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in GoldRateBackgroundService while fetching live market rates.");
                }

                await Task.Delay(Interval, stoppingToken);
            }

            _logger.LogInformation("Gold & Silver Live Market Continuous Worker Service stopped.");
        }
    }
}
