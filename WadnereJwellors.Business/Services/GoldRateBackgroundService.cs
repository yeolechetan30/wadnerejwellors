using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WadnereJwellors.DataAccess.Repositories;

namespace WadnereJwellors.Business.Services
{
    public class GoldRateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoldRateBackgroundService> _logger;

        public GoldRateBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<GoldRateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scheduledTimes = GetScheduledDailyTimes();
            var timesDisplay = string.Join(" and ", scheduledTimes.Select(t => DateTime.Today.Add(t).ToString("hh:mm tt")));
            _logger.LogInformation("Gold & Silver Daily Worker started. Scheduled to run twice daily at: {Times} (IST).", timesDisplay);

            // Startup check: if no rate exists for today, fetch once immediately
            await CheckAndFetchStartupRateAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var (delay, targetTime) = GetDelayUntilNextRun();
                _logger.LogInformation("Next gold & silver rate fetch scheduled for {TargetTime} (in {Hours}h {Minutes}m {Seconds}s).",
                    targetTime.ToString("hh:mm tt"), (int)delay.TotalHours, delay.Minutes, delay.Seconds);

                // Wait until the next scheduled time
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    _logger.LogInformation("Scheduled time reached ({Time}). Fetching gold and silver rates...", DateTime.Now.ToString("hh:mm tt"));
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var goldRateService = scope.ServiceProvider.GetRequiredService<IGoldRateService>();
                        await goldRateService.FetchAndBroadcastLiveRatesAsync();
                    }
                    _logger.LogInformation("Successfully updated and broadcasted daily metal rates.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in GoldRateBackgroundService while executing scheduled rate fetch.");
                }

                // Sleep 1 minute to prevent multiple triggers in the same scheduled minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Gold & Silver Daily Worker stopped.");
        }

        private async Task CheckAndFetchStartupRateAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IGoldRateRepository>();
                var latest = await repo.GetLatestRateAsync();

                var istZone = GetIstTimeZone();
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

                // If no rates exist or latest rate is from a previous day
                bool isMissingToday = latest == null || TimeZoneInfo.ConvertTimeFromUtc(latest.Timestamp, istZone).Date < nowIst.Date;

                if (isMissingToday)
                {
                    _logger.LogInformation("No rate found for today ({Date}). Fetching initial rate on startup...", nowIst.ToString("dd-MMM-yyyy"));
                    var service = scope.ServiceProvider.GetRequiredService<IGoldRateService>();
                    await service.FetchAndBroadcastLiveRatesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not perform initial startup rate check.");
            }
        }

        private (TimeSpan delay, DateTime targetTime) GetDelayUntilNextRun()
        {
            var istZone = GetIstTimeZone();
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
            var scheduledTimes = GetScheduledDailyTimes();

            DateTime? nextRunIst = null;

            // Check today's remaining scheduled times
            foreach (var time in scheduledTimes)
            {
                var targetDateTime = nowIst.Date.Add(time);
                if (nowIst < targetDateTime)
                {
                    nextRunIst = targetDateTime;
                    break;
                }
            }

            // If all times today have passed, pick the first scheduled time tomorrow
            if (!nextRunIst.HasValue)
            {
                var earliestTomorrow = scheduledTimes.First();
                nextRunIst = nowIst.Date.AddDays(1).Add(earliestTomorrow);
            }

            var delay = nextRunIst.Value - nowIst;
            return (delay > TimeSpan.Zero ? delay : TimeSpan.Zero, nextRunIst.Value);
        }

        private List<TimeSpan> GetScheduledDailyTimes()
        {
            var configuredTimes = _configuration.GetSection("GoldMarketSettings:DailyScheduleTimes").Get<string[]>();
            var list = new List<TimeSpan>();

            if (configuredTimes != null && configuredTimes.Length > 0)
            {
                foreach (var str in configuredTimes)
                {
                    if (TimeSpan.TryParse(str, out var parsed))
                    {
                        list.Add(parsed);
                    }
                }
            }

            // Default twice a day: 09:01 AM and 05:01 PM IST if none configured
            if (list.Count == 0)
            {
                list.Add(new TimeSpan(9, 1, 0));   // 09:01 AM
                list.Add(new TimeSpan(17, 1, 0));  // 05:01 PM
            }

            return list.OrderBy(t => t).ToList();
        }

        private static TimeZoneInfo GetIstTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time");
                }
            }
        }
    }
}
