using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Context;
using WadnereJwellors.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace WadnereJwellors.Business.Services
{
    public class CashfreePaymentService : ICashfreePaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CashfreePaymentService> _logger;
        private readonly ApplicationDbContext _dbContext;

        public CashfreePaymentService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CashfreePaymentService> logger,
            ApplicationDbContext dbContext) {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<PaymentOrderResponseDto> CreateOrderAsync(CreatePaymentRequestDto request) {
            var baseUrl = _configuration["CashfreeSettings:BaseUrl"]?.TrimEnd('/');
            var clientId = _configuration["CashfreeSettings:ClientId"];
            var clientSecret = _configuration["CashfreeSettings:ClientSecret"];
            var apiVersion = _configuration["CashfreeSettings:ApiVersion"] ?? "2023-08-01";
            var returnUrl = _configuration["CashfreeSettings:ReturnUrl"];
            var notifyUrl = _configuration["CashfreeSettings:NotifyUrl"];

            string orderId = $"ORDER_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(1000, 9999)}";
            string customerId = request.RegistrationId.HasValue ? $"CUST_{request.RegistrationId}" : $"CUST_{request.CustomerPhone}";

            var payload = new {
                order_id = orderId,
                order_amount = request.Amount,
                order_currency = "INR",
                order_note = request.OrderNote ?? "Jewellery Purchase",
                customer_details = new {
                    customer_id = customerId,
                    customer_name = request.CustomerName,
                    customer_email = request.CustomerEmail,
                    customer_phone = request.CustomerPhone
                },
                order_meta = new {
                    return_url = returnUrl,
                    notify_url = notifyUrl
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/orders");
            httpRequest.Headers.Add("x-client-id", clientId);
            httpRequest.Headers.Add("x-client-secret", clientSecret);
            httpRequest.Headers.Add("x-api-version", apiVersion);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                _logger.LogError("[Cashfree] Order creation failed ({StatusCode}): {Response}", response.StatusCode, responseContent);
                throw new InvalidOperationException($"Cashfree API error: {responseContent}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            string paymentSessionId = root.GetProperty("payment_session_id").GetString() ?? string.Empty;
            string? cfOrderId = root.TryGetProperty("cf_order_id", out var cfProp) ? cfProp.ToString() : null;

            // Save record in database
            var transaction = new PaymentTransaction {
                RegistrationId = request.RegistrationId,
                OrderId = orderId,
                CfOrderId = cfOrderId,
                PaymentSessionId = paymentSessionId,
                Amount = request.Amount,
                Currency = "INR",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Set<PaymentTransaction>().Add(transaction);
            await _dbContext.SaveChangesAsync();

            return new PaymentOrderResponseDto {
                Success = true,
                Message = "Cashfree order created successfully.",
                OrderId = orderId,
                PaymentSessionId = paymentSessionId,
                Amount = request.Amount,
                Currency = "INR"
            };
        }

        public async Task<PaymentStatusDto> GetOrderStatusAsync(string orderId) {
            var baseUrl = _configuration["CashfreeSettings:BaseUrl"]?.TrimEnd('/');
            var clientId = _configuration["CashfreeSettings:ClientId"];
            var clientSecret = _configuration["CashfreeSettings:ClientSecret"];
            var apiVersion = _configuration["CashfreeSettings:ApiVersion"] ?? "2023-08-01";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/orders/{orderId}");
            httpRequest.Headers.Add("x-client-id", clientId);
            httpRequest.Headers.Add("x-client-secret", clientSecret);
            httpRequest.Headers.Add("x-api-version", apiVersion);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                throw new InvalidOperationException($"Failed to fetch order status: {responseContent}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            string orderStatus = root.GetProperty("order_status").GetString() ?? "PENDING";
            decimal amount = root.GetProperty("order_amount").GetDecimal();

            // Update database status
            var transaction = await _dbContext.Set<PaymentTransaction>().FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (transaction != null) {
                transaction.Status = orderStatus;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return new PaymentStatusDto {
                OrderId = orderId,
                Status = orderStatus,
                Amount = amount
            };
        }

        public bool VerifyWebhookSignature(string rawBody, string signature, string timestamp) {
            var clientSecret = _configuration["CashfreeSettings:ClientSecret"];
            if (string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(signature)) return false;

            // Cashfree computes HMAC-SHA256 signature using (timestamp + rawBody)
            string signatureData = timestamp + rawBody;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            string computedSignature = Convert.ToBase64String(hash);

            return string.Equals(signature, computedSignature, StringComparison.Ordinal);
        }

        public async Task ProcessWebhookEventAsync(string rawBody) {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("order", out var order)) {
                string orderId = order.GetProperty("order_id").GetString() ?? string.Empty;
                string orderStatus = order.GetProperty("order_status").GetString() ?? string.Empty;

                var transaction = await _dbContext.Set<PaymentTransaction>().FirstOrDefaultAsync(x => x.OrderId == orderId);
                if (transaction != null) {
                    transaction.Status = orderStatus;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("[Cashfree Webhook] Updated Order {OrderId} status to {Status}", orderId, orderStatus);
                }
            }
        }
    }
}
