using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace WadnereJwellors.Business.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("[EmailService] Cannot send email: Recipient email address is null or empty.");
                return false;
            }

            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portString = _configuration["EmailSettings:Port"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "no-reply@wadnerejewellers.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Wadnere Jewellers";
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(smtpServer))
            {
                _logger.LogWarning("[EmailService] SmtpServer is not configured in EmailSettings. Skipping email delivery to {ToEmail}.", toEmail);
                return false;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("[EmailService] SMTP Username or Password is not configured in appsettings.json. Skipping email dispatch to {ToEmail}. Please configure your email credentials (e.g. Gmail App Password).", toEmail);
                return false;
            }

            int port = int.TryParse(portString, out var p) ? p : 587;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(toEmail, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Select appropriate SSL/TLS security options based on port
                SecureSocketOptions socketOptions = port switch
                {
                    465 => SecureSocketOptions.SslOnConnect,
                    587 => SecureSocketOptions.StartTls,
                    25 => SecureSocketOptions.StartTlsWhenAvailable,
                    _ => SecureSocketOptions.Auto
                };

                _logger.LogInformation("[EmailService] Connecting to SMTP server {SmtpServer}:{Port} (Security: {Security})...", smtpServer, port, socketOptions);
                await client.ConnectAsync(smtpServer, port, socketOptions);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogInformation("[EmailService] Authenticating with username {Username}...", username);
                    await client.AuthenticateAsync(username, password);
                }

                _logger.LogInformation("[EmailService] Dispatching email to {ToEmail} with subject '{Subject}'...", toEmail, subject);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("[EmailService] Successfully sent email to {ToEmail}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Failed to send email to {ToEmail} via {SmtpServer}:{Port}. Please ensure SMTP settings and App Password are valid.", toEmail, smtpServer, port);
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string purpose = "Login Verification")
        {
            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Valued Customer" : recipientName.Trim();
            var subject = $"{otpCode} is your Wadnere Jewellers {purpose} OTP";

            var htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Wadnere Jewellers - One Time Password</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f7f6f2;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333333;
        }}
        .container {{
            max-width: 600px;
            margin: 30px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 18px rgba(0, 0, 0, 0.08);
            border: 1px solid #e8e3d6;
        }}
        .header {{
            background: linear-gradient(135deg, #1f1f1f 0%, #2c2416 100%);
            padding: 30px 20px;
            text-align: center;
            border-bottom: 3px solid #d4af37;
        }}
        .header h1 {{
            margin: 0;
            color: #d4af37;
            font-size: 26px;
            font-weight: 700;
            letter-spacing: 2px;
            text-transform: uppercase;
        }}
        .header p {{
            margin: 6px 0 0 0;
            color: #d1c7b7;
            font-size: 13px;
            letter-spacing: 1px;
        }}
        .content {{
            padding: 35px 30px;
            line-height: 1.6;
        }}
        .greeting {{
            font-size: 18px;
            font-weight: 600;
            color: #222222;
            margin-bottom: 12px;
        }}
        .message-text {{
            font-size: 15px;
            color: #555555;
            margin-bottom: 24px;
        }}
        .otp-box {{
            background: linear-gradient(135deg, #fbf7ee 0%, #f4ebd5 100%);
            border: 2px dashed #d4af37;
            border-radius: 10px;
            text-align: center;
            padding: 20px;
            margin: 25px 0;
        }}
        .otp-label {{
            font-size: 13px;
            color: #7d6b38;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            font-weight: 600;
            margin-bottom: 8px;
        }}
        .otp-code {{
            font-size: 38px;
            font-weight: 800;
            color: #1a1a1a;
            letter-spacing: 8px;
            margin: 5px 0;
            font-family: 'Consolas', 'Courier New', monospace;
        }}
        .otp-expiry {{
            font-size: 12px;
            color: #8c7b55;
            margin-top: 6px;
        }}
        .warning-box {{
            background-color: #fff9e6;
            border-left: 4px solid #d4af37;
            padding: 12px 16px;
            border-radius: 4px;
            margin-top: 25px;
            font-size: 13px;
            color: #6d5b24;
        }}
        .footer {{
            background-color: #faf9f6;
            padding: 20px 30px;
            text-align: center;
            font-size: 12px;
            color: #888888;
            border-top: 1px solid #eeeeee;
        }}
        .footer p {{
            margin: 4px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Wadnere Jewellers</h1>
            <p>Purity & Trust Since Inception</p>
        </div>
        <div class=""content"">
            <div class=""greeting"">Hello {displayName},</div>
            <p class=""message-text"">
                You have requested a verification code for <strong>{purpose}</strong> on your Wadnere Jewellers account.
            </p>
            <div class=""otp-box"">
                <div class=""otp-label"">Your Verification Code</div>
                <div class=""otp-code"">{otpCode}</div>
                <div class=""otp-expiry"">&#9201; Valid for 5 minutes</div>
            </div>
            <div class=""warning-box"">
                <strong>Security Alert:</strong> Please do NOT share this OTP with anyone, including Wadnere Jewellers representatives. If you did not initiate this request, please change your password or contact support immediately.
            </div>
        </div>
        <div class=""footer"">
            <p>This is an automated message. Please do not reply directly to this email.</p>
            <p>&copy; {DateTime.UtcNow.Year} Wadnere Jewellers. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody, isHtml: true);
        }

        public async Task<bool> SendTransactionReceiptEmailAsync(
            string toEmail,
            string customerName,
            string orderId,
            decimal amount,
            string status,
            string? paymentMode = null,
            string? referenceId = null,
            string? failureReason = null)
        {
            var displayName = string.IsNullOrWhiteSpace(customerName) ? "Valued Customer" : customerName.Trim();
            bool isSuccess = status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);

            string subject = isSuccess
                ? $"Payment Receipt: ₹{amount:N2} for Order {orderId} - Wadnere Jewellers"
                : $"Payment Update: Order {orderId} ({status}) - Wadnere Jewellers";

            string statusColor = isSuccess ? "#28a745" : (status.Equals("PENDING", StringComparison.OrdinalIgnoreCase) ? "#ffc107" : "#dc3545");
            string statusBg = isSuccess ? "#d4edda" : (status.Equals("PENDING", StringComparison.OrdinalIgnoreCase) ? "#fff3cd" : "#f8d7da");
            string statusText = isSuccess ? "PAYMENT SUCCESSFUL" : status.ToUpperInvariant();

            string istTime = DateTime.UtcNow.AddHours(5).AddMinutes(30).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            var htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Wadnere Jewellers - Payment Receipt</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f7f6f2;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            color: #333333;
        }}
        .container {{
            max-width: 600px;
            margin: 30px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 18px rgba(0, 0, 0, 0.08);
            border: 1px solid #e8e3d6;
        }}
        .header {{
            background: linear-gradient(135deg, #1f1f1f 0%, #2c2416 100%);
            padding: 28px 20px;
            text-align: center;
            border-bottom: 3px solid #d4af37;
        }}
        .header h1 {{
            margin: 0;
            color: #d4af37;
            font-size: 24px;
            font-weight: 700;
            letter-spacing: 2px;
            text-transform: uppercase;
        }}
        .header p {{
            margin: 6px 0 0 0;
            color: #d1c7b7;
            font-size: 13px;
            letter-spacing: 1px;
        }}
        .content {{
            padding: 30px;
            line-height: 1.6;
        }}
        .status-badge {{
            display: inline-block;
            background-color: {statusBg};
            color: {statusColor};
            border: 1px solid {statusColor};
            font-weight: 700;
            font-size: 14px;
            padding: 8px 16px;
            border-radius: 20px;
            margin: 10px 0 20px 0;
            letter-spacing: 1px;
        }}
        .amount-card {{
            background: linear-gradient(135deg, #fbf7ee 0%, #f4ebd5 100%);
            border: 1px solid #d4af37;
            border-radius: 10px;
            padding: 20px;
            text-align: center;
            margin: 15px 0 25px 0;
        }}
        .amount-label {{
            font-size: 13px;
            color: #7d6b38;
            text-transform: uppercase;
            font-weight: 600;
            letter-spacing: 1px;
        }}
        .amount-value {{
            font-size: 32px;
            font-weight: 800;
            color: #1a1a1a;
            margin: 5px 0;
        }}
        .receipt-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        .receipt-table td {{
            padding: 12px 8px;
            border-bottom: 1px solid #eeeeee;
            font-size: 14px;
        }}
        .receipt-table td.label {{
            color: #777777;
            font-weight: 500;
            width: 40%;
        }}
        .receipt-table td.value {{
            color: #222222;
            font-weight: 600;
            text-align: right;
        }}
        .footer {{
            background-color: #faf9f6;
            padding: 20px 30px;
            text-align: center;
            font-size: 12px;
            color: #888888;
            border-top: 1px solid #eeeeee;
        }}
        .footer p {{
            margin: 4px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Wadnere Jewellers</h1>
            <p>Purity & Trust Since Inception</p>
        </div>
        <div class=""content"">
            <div style=""text-align: center;"">
                <span class=""status-badge"">{statusText}</span>
            </div>
            
            <p style=""font-size: 16px; margin: 0 0 10px 0;"">Dear <strong>{displayName}</strong>,</p>
            <p style=""color: #555555; margin: 0 0 20px 0;"">
                {(isSuccess ? "Thank you for your transaction with Wadnere Jewellers. Your payment has been received and verified successfully." : $"Your transaction status has been updated to {statusText}.") }
            </p>

            <div class=""amount-card"">
                <div class=""amount-label"">Total Amount</div>
                <div class=""amount-value"">&#8377;{amount:N2}</div>
                <div style=""font-size: 12px; color: #888888;"">Currency: INR</div>
            </div>

            <table class=""receipt-table"">
                <tr>
                    <td class=""label"">Order ID</td>
                    <td class=""value"">{orderId}</td>
                </tr>
                <tr>
                    <td class=""label"">Transaction Status</td>
                    <td class=""value"" style=""color: {statusColor};"">{statusText}</td>
                </tr>
                {(string.IsNullOrWhiteSpace(paymentMode) ? "" : $@"<tr><td class=""label"">Payment Mode</td><td class=""value"">{paymentMode.ToUpperInvariant()}</td></tr>")}
                {(string.IsNullOrWhiteSpace(referenceId) ? "" : $@"<tr><td class=""label"">Reference / UTR ID</td><td class=""value"">{referenceId}</td></tr>")}
                <tr>
                    <td class=""label"">Transaction Time</td>
                    <td class=""value"">{istTime}</td>
                </tr>
                {(!isSuccess && !string.IsNullOrWhiteSpace(failureReason) ? $@"<tr><td class=""label"">Reason</td><td class=""value"" style=""color: #dc3545;"">{failureReason}</td></tr>" : "")}
            </table>

            <div style=""background-color: #faf9f6; border-radius: 8px; padding: 15px; margin-top: 25px; font-size: 13px; color: #666666;"">
                <strong>Need Help?</strong> If you have any questions regarding this transaction or your order, please contact our support team at <a href=""mailto:support@wadnerejewellers.com"" style=""color: #d4af37;"">support@wadnerejewellers.com</a>.
            </div>
        </div>
        <div class=""footer"">
            <p>This is an automated payment receipt from Wadnere Jewellers.</p>
            <p>&copy; {DateTime.UtcNow.Year} Wadnere Jewellers. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody, isHtml: true);
        }
    }
}
