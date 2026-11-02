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
        private const decimal TROY_OUNCE_TO_GRAMS = 31.1034768m;
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
                var apiUrl = _configuration["GoldMarketSettings:ApiUrl"] ?? "https://api.gold-api.com/price";
                var baseUrl = apiUrl.TrimEnd('/');

                var adjustmentFactorStr = _configuration["GoldMarketSettings:IndianMarketAdjustmentFactor"];
                decimal indianMarketFactor = decimal.TryParse(adjustmentFactorStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var f) ? f : 1.158m;

                decimal gold24K = 7650.00m;
                decimal gold22K = 7015.00m;
                decimal gold18K = 5738.00m;
                decimal silverPerGram = 93.50m;
                string source = "gold-api.com";

                // 1. Fetch Gold (XAU) rate in INR from https://api.gold-api.com/price/XAU/INR
                try
                {
                    var goldUrl = $"{baseUrl}/XAU/INR";
                    var goldResponse = await _httpClient.GetAsync(goldUrl);

                    if (goldResponse.IsSuccessStatusCode)
                    {
                        var json = await goldResponse.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("price", out var priceProp))
                        {
                            decimal goldOuncePrice = priceProp.GetDecimal();
                            decimal goldGramBase = goldOuncePrice / TROY_OUNCE_TO_GRAMS;

                            // Apply Indian retail market factor (import duty + GST + local premium)
                            gold24K = Math.Round(goldGramBase * indianMarketFactor, 2);
                            gold22K = Math.Round(gold24K * (22.0m / 24.0m), 2); // 91.6% purity
                            gold18K = Math.Round(gold24K * (18.0m / 24.0m), 2); // 75.0% purity
                        }
                    }
                    else
                    {
                        var errText = await goldResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("[gold-api.com] Gold fetch status {Status}: {Error}", goldResponse.StatusCode, errText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[gold-api.com] Failed to fetch Gold (XAU) rates.");
                }

                // 2. Fetch Silver (XAG) rate in INR from https://api.gold-api.com/price/XAG/INR
                try
                {
                    var silverUrl = $"{baseUrl}/XAG/INR";
                    var silverResponse = await _httpClient.GetAsync(silverUrl);

                    if (silverResponse.IsSuccessStatusCode)
                    {
                        var json = await silverResponse.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("price", out var priceProp))
                        {
                            decimal silverOuncePrice = priceProp.GetDecimal();
                            decimal silverGramBase = silverOuncePrice / TROY_OUNCE_TO_GRAMS;

                            // Apply Indian market factor
                            silverPerGram = Math.Round(silverGramBase * indianMarketFactor, 2);
                        }
                    }
                    else
                    {
                        var errText = await silverResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("[gold-api.com] Silver fetch status {Status}: {Error}", silverResponse.StatusCode, errText);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[gold-api.com] Failed to fetch Silver (XAG) rates.");
                }

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

                // Broadcast rate update to all connected SignalR clients
                await _hubContext.Clients.All.SendAsync("ReceiveRateUpdate", rateDto);
                _logger.LogInformation("[{Source}] Broadcasted Gold 24K: ₹{Gold24K}/g, 22K: ₹{Gold22K}/g, 18K: ₹{Gold18K}/g, Silver: ₹{Silver}/g", source, gold24K, gold22K, gold18K, silverPerGram);

                return rateDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[gold-api.com] Exception while fetching metal rates.");
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
