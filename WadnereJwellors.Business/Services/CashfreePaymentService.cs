using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Context;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.Business.Services
{
    public class CashfreePaymentService : ICashfreePaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CashfreePaymentService> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public CashfreePaymentService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CashfreePaymentService> logger,
            ApplicationDbContext dbContext,
            IEmailService emailService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
            _emailService = emailService;
        }

        public async Task<PaymentOrderResponseDto> CreateOrderAsync(CreatePaymentRequestDto request)
        {
            var baseUrl = _configuration["CashfreeSettings:BaseUrl"]?.TrimEnd('/');
            var clientId = _configuration["CashfreeSettings:ClientId"];
            var clientSecret = _configuration["CashfreeSettings:ClientSecret"];
            var apiVersion = _configuration["CashfreeSettings:ApiVersion"] ?? "2023-08-01";
            var returnUrl = _configuration["CashfreeSettings:ReturnUrl"];
            var notifyUrl = _configuration["CashfreeSettings:NotifyUrl"];

            string orderId = $"ORDER_{DateTime.UtcNow:yyyyMMddHHmmss}_{RandomNumberGenerator.GetInt32(1000, 9999)}";
            string customerId = request.RegistrationId.HasValue ? $"CUST_{request.RegistrationId}" : $"CUST_{request.CustomerPhone}";

            var payload = new
            {
                order_id = orderId,
                order_amount = request.Amount,
                order_currency = "INR",
                order_note = request.OrderNote ?? "Jewellery Purchase",
                customer_details = new
                {
                    customer_id = customerId,
                    customer_name = request.CustomerName,
                    customer_email = request.CustomerEmail,
                    customer_phone = request.CustomerPhone
                },
                order_meta = new
                {
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

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Cashfree] Order creation failed ({StatusCode}): {Response}", response.StatusCode, responseContent);
                throw new InvalidOperationException($"Cashfree API error: {responseContent}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            string paymentSessionId = root.GetProperty("payment_session_id").GetString() ?? string.Empty;
            string? cfOrderId = root.TryGetProperty("cf_order_id", out var cfProp) ? cfProp.ToString() : null;

            // Save record in database
            var transaction = new PaymentTransaction
            {
                RegistrationId = request.RegistrationId,
                OrderId = orderId,
                CfOrderId = cfOrderId,
                PaymentSessionId = paymentSessionId,
                Amount = request.Amount,
                Currency = "INR",
                Status = "PENDING",
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                OrderNote = request.OrderNote,
                IsEmailSent = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Set<PaymentTransaction>().Add(transaction);
            await _dbContext.SaveChangesAsync();

            return new PaymentOrderResponseDto
            {
                Success = true,
                Message = "Cashfree order created successfully.",
                OrderId = orderId,
                PaymentSessionId = paymentSessionId,
                Amount = request.Amount,
                Currency = "INR"
            };
        }

        public async Task<PaymentStatusDto> GetOrderStatusAsync(string orderId)
        {
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

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to fetch order status: {responseContent}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            string orderStatus = root.GetProperty("order_status").GetString() ?? "PENDING";
            decimal amount = root.GetProperty("order_amount").GetDecimal();

            // Update database status
            var transaction = await _dbContext.Set<PaymentTransaction>().FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (transaction != null)
            {
                transaction.Status = orderStatus;
                transaction.UpdatedAt = DateTime.UtcNow;

                if (orderStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase) || orderStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                {
                    bool shouldSendEmail = transaction.PaymentCompletionTime == null && !string.IsNullOrWhiteSpace(transaction.CustomerEmail);
                    transaction.PaymentCompletionTime ??= DateTime.UtcNow;

                    if (shouldSendEmail)
                    {
                        try
                        {
                            await _emailService.SendTransactionReceiptEmailAsync(
                                transaction.CustomerEmail!,
                                transaction.CustomerName ?? "Valued Customer",
                                transaction.OrderId,
                                transaction.Amount,
                                transaction.Status,
                                transaction.PaymentMode,
                                transaction.ReferenceId,
                                transaction.FailureReason
                            );
                            transaction.IsEmailSent = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[Cashfree] Failed to dispatch transaction email for Order {OrderId}", orderId);
                        }
                    }

                    // Fulfill Membership or Monthly Payment
                    await FulfillPaidOrderAsync(transaction);
                }

                await _dbContext.SaveChangesAsync();
            }

            return new PaymentStatusDto
            {
                OrderId = orderId,
                Status = orderStatus,
                Amount = amount
            };
        }

        public bool VerifyWebhookSignature(string rawBody, string signature, string timestamp)
        {
            var clientSecret = _configuration["CashfreeSettings:ClientSecret"];
            if (string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(signature)) return false;

            // Cashfree computes HMAC-SHA256 signature using (timestamp + rawBody)
            string signatureData = timestamp + rawBody;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            string computedSignature = Convert.ToBase64String(hash);

            return string.Equals(signature, computedSignature, StringComparison.Ordinal);
        }

        public async Task ProcessWebhookEventAsync(string rawBody)
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            string eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "" : "";

            if (root.TryGetProperty("data", out var data))
            {
                string orderId = string.Empty;
                string orderStatus = string.Empty;
                string? paymentMode = null;
                string? referenceId = null;
                string? failureReason = null;

                if (data.TryGetProperty("order", out var order))
                {
                    orderId = order.TryGetProperty("order_id", out var oId) ? oId.GetString() ?? "" : "";
                    orderStatus = order.TryGetProperty("order_status", out var oStat) ? oStat.GetString() ?? "" : "";
                }

                if (data.TryGetProperty("payment", out var payment))
                {
                    if (string.IsNullOrEmpty(orderStatus) && payment.TryGetProperty("payment_status", out var pStat))
                    {
                        orderStatus = pStat.GetString() ?? "";
                    }
                    if (payment.TryGetProperty("payment_group", out var pGrp))
                    {
                        paymentMode = pGrp.GetString();
                    }
                    if (payment.TryGetProperty("bank_reference", out var bRef))
                    {
                        referenceId = bRef.GetString();
                    }
                    else if (payment.TryGetProperty("cf_payment_id", out var cfPayId))
                    {
                        referenceId = cfPayId.ToString();
                    }
                    if (payment.TryGetProperty("payment_message", out var pMsg))
                    {
                        failureReason = pMsg.GetString();
                    }
                }

                if (string.IsNullOrWhiteSpace(orderStatus))
                {
                    if (eventType.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) || eventType.Contains("PAID", StringComparison.OrdinalIgnoreCase))
                    {
                        orderStatus = "PAID";
                    }
                    else if (eventType.Contains("FAILED", StringComparison.OrdinalIgnoreCase))
                    {
                        orderStatus = "FAILED";
                    }
                    else if (eventType.Contains("DROPPED", StringComparison.OrdinalIgnoreCase))
                    {
                        orderStatus = "USER_DROPPED";
                    }
                }

                if (!string.IsNullOrEmpty(orderId))
                {
                    var transaction = await _dbContext.Set<PaymentTransaction>().FirstOrDefaultAsync(x => x.OrderId == orderId);
                    if (transaction != null)
                    {
                        transaction.Status = orderStatus;
                        if (!string.IsNullOrEmpty(paymentMode)) transaction.PaymentMode = paymentMode;
                        if (!string.IsNullOrEmpty(referenceId)) transaction.ReferenceId = referenceId;
                        if (!string.IsNullOrEmpty(failureReason)) transaction.FailureReason = failureReason;
                        transaction.UpdatedAt = DateTime.UtcNow;

                        if (orderStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase) || orderStatus.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                        {
                            bool shouldSendEmail = transaction.PaymentCompletionTime == null && !string.IsNullOrWhiteSpace(transaction.CustomerEmail);
                            transaction.PaymentCompletionTime ??= DateTime.UtcNow;

                            if (shouldSendEmail)
                            {
                                try
                                {
                                    await _emailService.SendTransactionReceiptEmailAsync(
                                        transaction.CustomerEmail!,
                                        transaction.CustomerName ?? "Valued Customer",
                                        transaction.OrderId,
                                        transaction.Amount,
                                        transaction.Status,
                                        transaction.PaymentMode,
                                        transaction.ReferenceId,
                                        transaction.FailureReason
                                    );
                                    transaction.IsEmailSent = true;
                                    _logger.LogInformation("[Cashfree Webhook] Transaction receipt email sent to {Email} for Order {OrderId}", transaction.CustomerEmail, transaction.OrderId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "[Cashfree Webhook] Failed to dispatch transaction email to {Email}", transaction.CustomerEmail);
                                }
                            }

                            // Fulfill Membership or Monthly Payment
                            await FulfillPaidOrderAsync(transaction);
                        }

                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("[Cashfree Webhook] Updated Order {OrderId} status to {Status}", orderId, orderStatus);
                    }
                }
            }
        }

        private async Task FulfillPaidOrderAsync(PaymentTransaction transaction)
        {
            if (!transaction.RegistrationId.HasValue)
            {
                _logger.LogWarning("[Fulfillment] Cannot fulfill order {OrderId} because RegistrationId is null.", transaction.OrderId);
                return;
            }

            // 1. Membership Fulfillment (OrderNote contains "MEMBERSHIP")
            if (transaction.OrderNote != null && transaction.OrderNote.Contains("MEMBERSHIP", StringComparison.OrdinalIgnoreCase))
            {
                bool exists = await _dbContext.UserMemberships.AnyAsync(m => m.PaymentTransactionId == transaction.Id);
                if (!exists)
                {
                    int currentYear = DateTime.UtcNow.Year;
                    string prefix = $"mem_{currentYear}";
                    int count = await _dbContext.UserMemberships.CountAsync(m => m.MembershipCode.StartsWith(prefix));
                    string code = $"{prefix}{(count + 1):D4}";

                    int validityYears = _configuration.GetValue<int>("MembershipSettings:ValidityInYears", 1);
                    var now = DateTime.UtcNow;

                    var membership = new UserMembership
                    {
                        RegistrationId = transaction.RegistrationId.Value,
                        MembershipCode = code,
                        Amount = transaction.Amount,
                        FromDate = now,
                        ToDate = now.AddYears(validityYears),
                        IsActive = true,
                        PaymentTransactionId = transaction.Id,
                        CreatedDate = now
                    };
                    _dbContext.UserMemberships.Add(membership);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("[Fulfillment] Membership {Code} created for user {UserId}", code, transaction.RegistrationId.Value);
                }
            }
            // 2. Monthly Payment Fulfillment (OrderNote contains "MONTHLY")
            else if (transaction.OrderNote != null && transaction.OrderNote.Contains("MONTHLY", StringComparison.OrdinalIgnoreCase))
            {
                bool exists = await _dbContext.MonthlyPayments.AnyAsync(m => m.PaymentTransactionId == transaction.Id);
                if (!exists)
                {
                    var now = DateTime.UtcNow;
                    var monthlyPayment = new MonthlyPayment
                    {
                        RegistrationId = transaction.RegistrationId.Value,
                        PaymentTransactionId = transaction.Id,
                        Amount = transaction.Amount,
                        PaymentDate = now,
                        PaymentMonth = now.Month,
                        PaymentYear = now.Year,
                        MonthYearDisplay = now.ToString("MMMM yyyy"),
                        IsRedeemed = false,
                        CreatedDate = now
                    };
                    _dbContext.MonthlyPayments.Add(monthlyPayment);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("[Fulfillment] Monthly payment for {Month} created for user {UserId}", monthlyPayment.MonthYearDisplay, transaction.RegistrationId.Value);
                }
            }
        }
    }
}
