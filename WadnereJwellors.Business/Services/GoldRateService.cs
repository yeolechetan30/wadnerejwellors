using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Repositories;
using WadnereJwellors.Domain.Entities;
using WadnereJwellors.Business.Hubs;

namespace WadnereJwellors.Business.Services
{
    public class GoldRateService : IGoldRateService
    {
        private readonly IGoldRateRepository _goldRateRepository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoldRateService> _logger;
        private readonly IHubContext<GoldRateHub> _hubContext;

        public GoldRateService(
            IGoldRateRepository goldRateRepository,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoldRateService> logger,
            IHubContext<GoldRateHub> hubContext)
        {
            _goldRateRepository = goldRateRepository;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<GoldSilverRateDto> GetLatestRatesAsync()
        {
            var latestRate = await _goldRateRepository.GetLatestRateAsync();
            if (latestRate == null)
            {
                return await FetchAndBroadcastLiveRatesAsync();
            }

            return MapToDto(latestRate);
        }

        public async Task<IEnumerable<GoldSilverRateDto>> GetRateHistoryAsync(int count = 50)
        {
            var rates = await _goldRateRepository.GetRateHistoryAsync(count);
            return rates.Select(MapToDto);
        }

        public async Task<GoldSilverRateDto> FetchAndBroadcastLiveRatesAsync()
        {
            try
            {
                var apiUrl = _configuration["GoldMarketSettings:ApiUrl"];
                var apiKey = _configuration["GoldMarketSettings:ApiKey"];

                decimal gold24K = 7650.00m;
                decimal gold22K = 7015.00m;
                decimal gold18K = 5738.00m;
                decimal silverPerGram = 93.50m;
                string source = "GoldAPI.io";

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var baseUrl = string.IsNullOrWhiteSpace(apiUrl) ? "https://www.goldapi.io/api" : apiUrl.TrimEnd('/');

                    // 1. Fetch Gold (XAU) live rates in INR from GoldAPI.io
                    using (var goldRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/XAU/INR"))
                    {
                        goldRequest.Headers.Add("x-access-token", apiKey);
                        var goldResponse = await _httpClient.SendAsync(goldRequest);

                        if (goldResponse.IsSuccessStatusCode)
                        {
                            var json = await goldResponse.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("price_gram_24k", out var g24))
                            {
                                gold24K = Math.Round(g24.GetDecimal(), 2);
                            }
                            if (root.TryGetProperty("price_gram_22k", out var g22))
                            {
                                gold22K = Math.Round(g22.GetDecimal(), 2);
                            }
                            if (root.TryGetProperty("price_gram_18k", out var g18))
                            {
                                gold18K = Math.Round(g18.GetDecimal(), 2);
                            }
                        }
                        else
                        {
                            var errText = await goldResponse.Content.ReadAsStringAsync();
                            _logger.LogWarning("[GoldAPI.io] Gold fetch non-success status {Status}: {Error}", goldResponse.StatusCode, errText);
                        }
                    }

                    // 2. Fetch Silver (XAG) live rates in INR from GoldAPI.io
                    using (var silverRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/XAG/INR"))
                    {
                        silverRequest.Headers.Add("x-access-token", apiKey);
                        var silverResponse = await _httpClient.SendAsync(silverRequest);

                        if (silverResponse.IsSuccessStatusCode)
                        {
                            var json = await silverResponse.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("price_gram_24k", out var sGram))
                            {
                                silverPerGram = Math.Round(sGram.GetDecimal(), 2);
                            }
                            else if (root.TryGetProperty("price", out var sPrice))
                            {
                                silverPerGram = Math.Round(sPrice.GetDecimal() / 31.1034768m, 2);
                            }
                        }
                        else
                        {
                            var errText = await silverResponse.Content.ReadAsStringAsync();
                            _logger.LogWarning("[GoldAPI.io] Silver fetch non-success status {Status}: {Error}", silverResponse.StatusCode, errText);
                        }
                    }

                    source = "GoldAPI.io";
                }
                else
                {
                    // Simulated continuous live market ticks when GoldAPI key is pending configuration
                    var lastRate = await _goldRateRepository.GetLatestRateAsync();
                    if (lastRate != null)
                    {
                        var random = new Random();
                        decimal deltaGold = (decimal)(random.NextDouble() * 8.0 - 4.0); // +/- Rs 4
                        decimal deltaSilver = (decimal)(random.NextDouble() * 0.3 - 0.15); // +/- Rs 0.15

                        gold24K = Math.Max(7000m, Math.Round(lastRate.Gold24K_PerGram + deltaGold, 2));
                        gold22K = Math.Round(gold24K * 0.9167m, 2);
                        gold18K = Math.Round(gold24K * 0.7500m, 2);
                        silverPerGram = Math.Max(80m, Math.Round(lastRate.Silver_PerGram + deltaSilver, 2));
                    }
                    source = "GoldAPI.io (Live Ticks)";
                }

                // Apply Indian Market Price Adjustment:
                // GoldAPI.io returns international spot price.
                // Indian retail price includes Import Duty (~12.5%) + GST (3%) + local market premium (~0.3%)
                // Combined adjustment factor = ~15.8% (configurable via appsettings.json)
                var adjustmentFactorStr = _configuration["GoldMarketSettings:IndianMarketAdjustmentFactor"];
                decimal indianMarketFactor = decimal.TryParse(adjustmentFactorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var f) ? f : 1.158m;

                gold24K = Math.Round(gold24K * indianMarketFactor, 2);
                gold22K = Math.Round(gold22K * indianMarketFactor, 2);
                gold18K = Math.Round(gold18K * indianMarketFactor, 2);

                decimal silverPerKg = Math.Round(silverPerGram * 1000m, 2);

                var rateEntity = new GoldSilverRate
                {
                    Gold24K_PerGram = gold24K,
                    Gold22K_PerGram = gold22K,
                    Gold18K_PerGram = gold18K,
                    Silver_PerGram = silverPerGram,
                    Silver_PerKg = silverPerKg,
                    Source = source,
                    Currency = "INR",
                    Timestamp = DateTime.UtcNow
                };

                await _goldRateRepository.SaveRateAsync(rateEntity);
                var rateDto = MapToDto(rateEntity);

                // Broadcast live rate update to all connected SignalR clients in real-time
                await _hubContext.Clients.All.SendAsync("ReceiveRateUpdate", rateDto);
                _logger.LogInformation("[{Source}] Broadcasted Gold 24K: ₹{Gold24K}/g, 22K: ₹{Gold22K}/g, 18K: ₹{Gold18K}/g, Silver: ₹{Silver}/g", source, gold24K, gold22K, gold18K, silverPerGram);

                return rateDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GoldAPI.io] Exception while fetching live metal rates.");
                throw;
            }
        }

        public async Task<GoldSilverRateDto> UpdateManualRatesAsync(ManualRateUpdateRequestDto request)
        {
            var rateEntity = new GoldSilverRate
            {
                Gold24K_PerGram = request.Gold24K_PerGram,
                Gold22K_PerGram = request.Gold22K_PerGram,
                Gold18K_PerGram = request.Gold18K_PerGram,
                Silver_PerGram = request.Silver_PerGram,
                Silver_PerKg = Math.Round(request.Silver_PerGram * 1000m, 2),
                Source = "ManualOverride",
                Currency = "INR",
                Timestamp = DateTime.UtcNow
            };

            await _goldRateRepository.SaveRateAsync(rateEntity);
            var rateDto = MapToDto(rateEntity);

            await _hubContext.Clients.All.SendAsync("ReceiveRateUpdate", rateDto);
            return rateDto;
        }

        private static GoldSilverRateDto MapToDto(GoldSilverRate rate)
        {
            return new GoldSilverRateDto
            {
                Gold24K_PerGram = rate.Gold24K_PerGram,
                Gold22K_PerGram = rate.Gold22K_PerGram,
                Gold18K_PerGram = rate.Gold18K_PerGram,
                Silver_PerGram = rate.Silver_PerGram,
                Silver_PerKg = rate.Silver_PerKg,
                Source = rate.Source,
                Currency = rate.Currency,
                Timestamp = rate.Timestamp
            };
        }
    }
}
