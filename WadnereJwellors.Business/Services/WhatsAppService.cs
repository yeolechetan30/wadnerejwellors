using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WadnereJwellors.Business.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WhatsAppService(ILogger<WhatsAppService> logger, IConfiguration configuration, HttpClient httpClient) {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> SendOtpViaWhatsAppAsync(long mobileNumber, string otpCode) {
            try {
                // 1. Format mobile number (e.g. "919975063087")
                var formattedMobile = FormatMobileNumberForMeta(mobileNumber);

                _logger.LogInformation("[Meta WhatsApp Cloud API] Initiating OTP {OtpCode} to {MobileNumber}", otpCode, formattedMobile);

                // 2. Fetch and Validate Configuration
                var accessToken = _configuration["WhatsAppSettings:AccessToken"]
                    ?? _configuration["WhatsAppSettings:ApiKey"];
                var phoneNumberId = _configuration["WhatsAppSettings:PhoneNumberId"];
                var templateName = _configuration["WhatsAppSettings:TemplateName"];

                if (string.IsNullOrWhiteSpace(phoneNumberId)) {
                    _logger.LogError("[Meta WhatsApp Cloud API] Missing 'PhoneNumberId' in appsettings.json.");
                    throw new InvalidOperationException("WhatsAppSettings:PhoneNumberId is required.");
                }

                if (string.IsNullOrWhiteSpace(accessToken)) {
                    _logger.LogError("[Meta WhatsApp Cloud API] Missing 'AccessToken' in appsettings.json.");
                    throw new InvalidOperationException("WhatsAppSettings:AccessToken is required.");
                }

                // 3. Build API Endpoint
                var apiUrl = $"https://graph.facebook.com/v20.0/{phoneNumberId.Trim()}/messages";

                // 4. Build Meta Template Payload (for standard OTP / verify_code templates)
                var payload = new {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = formattedMobile,
                    type = "template",
                    template = new {
                        name = !string.IsNullOrWhiteSpace(templateName) ? templateName : "hello_world",
                        language = new { code = "en_US" },
                    }
                };

                // 5. Send HTTP Request
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var jsonString = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode) {
                    _logger.LogInformation("[Meta WhatsApp Cloud API] OTP successfully sent. Meta Response: {Response}", responseContent);
                    return true;
                }
                else {
                    _logger.LogError("[Meta WhatsApp Cloud API] Failed to send OTP. Status: {StatusCode}, Error: {Error}", response.StatusCode, responseContent);
                    throw new InvalidOperationException($"Meta WhatsApp API error ({response.StatusCode}): {responseContent}");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[Meta WhatsApp Cloud API] Exception occurred while sending WhatsApp OTP to {MobileNumber}", mobileNumber);
                throw;
            }
        }

        //public async Task<bool> SendOtpViaWhatsAppAsync(long mobileNumber, string otpCode)
        //{
        //    try
        //    {
        //        // Format for Meta Cloud API: Digits only with country code (e.g. 919876543210)
        //        var formattedMobile = FormatMobileNumberForMeta(mobileNumber);
        //        var messageText = $"Dear Wadnere Jewellers Customer, your 6-digit 2FA login verification code is: {otpCode}. Valid for 5 minutes. Do not share this code with anyone.";

        //        // Log the OTP action for tracking/debugging
        //        _logger.LogInformation("[Meta WhatsApp Cloud API] Sending 6-digit OTP {OtpCode} to mobile number {MobileNumber}", otpCode, formattedMobile);

        //        var accessToken = _configuration["WhatsAppSettings:AccessToken"] 
        //            ?? _configuration["WhatsAppSettings:ApiKey"];
        //        var phoneNumberId = _configuration["WhatsAppSettings:PhoneNumberId"];
        //        var templateName = _configuration["WhatsAppSettings:TemplateName"];

        //        var apiUrl = _configuration["WhatsAppSettings:ApiUrl"];
        //        if (string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(phoneNumberId))
        //        {
        //            apiUrl = $"https://graph.facebook.com/v25.0/{phoneNumberId}/messages";
        //        }

        //        // If Meta WhatsApp Cloud API credentials are configured, send HTTP POST to Meta
        //        if (!string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(accessToken))
        //        {
        //            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        //            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //            object payload;
        //            if (!string.IsNullOrWhiteSpace(templateName))
        //            {
        //                // Official Meta Template Message Payload (e.g. verify_code template)
        //                payload = new
        //                {
        //                    messaging_product = "whatsapp",
        //                    recipient_type = "individual",
        //                    to = formattedMobile,
        //                    type = "template",
        //                    template = new
        //                    {
        //                        name = templateName,
        //                        language = new { code = "en_US" },
        //                        components = new object[]
        //                        {
        //                            new
        //                            {
        //                                type = "body",
        //                                parameters = new[]
        //                                {
        //                                    new { type = "text", text = otpCode }
        //                                }
        //                            }
        //                        }
        //                    }
        //                };
        //            }
        //            else
        //            {
        //                // Direct Meta Text Message Payload
        //                payload = new
        //                {
        //                    messaging_product = "whatsapp",
        //                    recipient_type = "individual",
        //                    to = formattedMobile,
        //                    type = "text",
        //                    text = new
        //                    {
        //                        preview_url = false,
        //                        body = messageText
        //                    }
        //                };
        //            }

        //            var jsonString = JsonSerializer.Serialize(payload);
        //            request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        //            var response = await _httpClient.SendAsync(request);
        //            var responseContent = await response.Content.ReadAsStringAsync();

        //            if (response.IsSuccessStatusCode)
        //            {
        //                _logger.LogInformation("[Meta WhatsApp Cloud API] OTP successfully sent. Meta Response: {Response}", responseContent);
        //                return true;
        //            }
        //            else
        //            {
        //                _logger.LogWarning("[Meta WhatsApp Cloud API] Failed to send WhatsApp message. Status: {StatusCode}, Error: {Error}", response.StatusCode, responseContent);

        //                // If template message failed (e.g. template parameter mismatch), try direct text message fallback
        //                if (!string.IsNullOrWhiteSpace(templateName))
        //                {
        //                    _logger.LogInformation("[Meta WhatsApp Cloud API] Attempting direct text message fallback...");
        //                    return await SendDirectTextMessageAsync(apiUrl, accessToken, formattedMobile, messageText);
        //                }

        //                throw new InvalidOperationException($"Meta WhatsApp API error: {responseContent}");
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "[Meta WhatsApp Cloud API] Exception occurred while sending WhatsApp OTP to {MobileNumber}", mobileNumber);
        //        throw;
        //    }
        //}

        private async Task<bool> SendDirectTextMessageAsync(string apiUrl, string accessToken, string formattedMobile, string messageText) {
            try {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var payload = new {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = formattedMobile,
                    type = "text",
                    text = new {
                        preview_url = false,
                        body = messageText
                    }
                };

                var jsonString = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode) {
                    _logger.LogInformation("[Meta WhatsApp Cloud API] Direct text message fallback succeeded. Response: {Response}", responseContent);
                    return true;
                }
                else {
                    _logger.LogWarning("[Meta WhatsApp Cloud API] Direct text message fallback failed. Status: {StatusCode}, Error: {Error}", response.StatusCode, responseContent);
                    throw new InvalidOperationException($"Meta WhatsApp API error: {responseContent}");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[Meta WhatsApp Cloud API] Fallback exception for mobile {Mobile}", formattedMobile);
                throw;
            }
        }

        private static string FormatMobileNumberForMeta(long mobileNumber) {
            var mobileStr = mobileNumber.ToString().Trim();
            if (mobileStr.StartsWith("+")) {
                mobileStr = mobileStr.Substring(1);
            }

            // If 10 digits Indian mobile number, prepend 91 country code
            if (mobileStr.Length == 10) {
                return "91" + mobileStr;
            }

            return mobileStr;
        }
    }
}
