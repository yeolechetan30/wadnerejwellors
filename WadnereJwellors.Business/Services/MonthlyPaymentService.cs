using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Context;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.Business.Services
{
    public class MonthlyPaymentService : IMonthlyPaymentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ICashfreePaymentService _paymentService;

        public MonthlyPaymentService(
            ApplicationDbContext dbContext,
            ICashfreePaymentService paymentService)
        {
            _dbContext = dbContext;
            _paymentService = paymentService;
        }

        public async Task<PaymentOrderResponseDto> InitiateMonthlyPaymentOrderAsync(MonthlyPaymentRequestDto request)
        {
            // 1. Fetch customer by RegistrationId
            var customer = await _dbContext.UserRegistrations
                .FirstOrDefaultAsync(u => u.RegistrationId == request.RegistrationId);

            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with RegistrationId {request.RegistrationId} not found.");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Monthly payment amount must be greater than zero.");
            }

            // 2. Create Cashfree payment order tagged with PURPOSE_MONTHLY_PAYMENT
            var orderRequest = new CreatePaymentRequestDto
            {
                RegistrationId = customer.RegistrationId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                CustomerEmail = customer.EmailAddress ?? string.Empty,
                CustomerPhone = customer.MobileNumber.ToString(),
                Amount = request.Amount,
                OrderNote = "PURPOSE_MONTHLY_PAYMENT"
            };

            return await _paymentService.CreateOrderAsync(orderRequest);
        }

        public async Task<int> RecordSuccessfulPaymentAsync(int registrationId, int transactionId, decimal amount, string? remarks = null)
        {
            var now = DateTime.UtcNow;

            var payment = new MonthlyPayment
            {
                RegistrationId = registrationId,
                PaymentTransactionId = transactionId,
                Amount = amount,
                PaymentDate = now,
                PaymentMonth = now.Month,
                PaymentYear = now.Year,
                MonthYearDisplay = now.ToString("MMMM yyyy"), // e.g. "October 2026"
                IsRedeemed = false,                          // default 0
                RedeemedDate = null,
                Remarks = remarks,
                CreatedDate = now
            };

            _dbContext.MonthlyPayments.Add(payment);
            await _dbContext.SaveChangesAsync();

            return payment.Id;
        }

        public async Task<UserPassbookResponseDto> GetPassbookAsync(int registrationId)
        {
            var customer = await _dbContext.UserRegistrations
                .FirstOrDefaultAsync(u => u.RegistrationId == registrationId);

            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with RegistrationId {registrationId} not found.");
            }

            var now = DateTime.UtcNow;

            // 1. Fetch active/latest membership for this customer
            var membership = await _dbContext.UserMemberships
                .Where(m => m.RegistrationId == registrationId)
                .OrderByDescending(m => m.CreatedDate)
                .FirstOrDefaultAsync();

            // 2. Fetch all monthly payments for this customer
            var payments = await _dbContext.MonthlyPayments
                .Include(p => p.PaymentTransaction)
                .Where(p => p.RegistrationId == registrationId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // 3. Build ledger entries
            var entries = payments.Select(p => new PassbookEntryDto
            {
                PaymentId = p.Id,
                MonthYearDisplay = p.MonthYearDisplay,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                OrderId = p.PaymentTransaction?.OrderId,
                ReferenceId = p.PaymentTransaction?.ReferenceId,
                IsRedeemed = p.IsRedeemed,
                RedeemedDate = p.RedeemedDate
            }).ToList();

            // 4. Calculate passbook metrics
            decimal totalSaved = entries.Sum(e => e.Amount);
            decimal totalRedeemed = entries.Where(e => e.IsRedeemed).Sum(e => e.Amount);
            decimal availableBalance = totalSaved - totalRedeemed;

            return new UserPassbookResponseDto
            {
                RegistrationId = customer.RegistrationId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                MembershipCode = membership?.MembershipCode,
                MemberSince = membership?.FromDate,
                MembershipValidTill = membership?.ToDate,
                IsMembershipActive = membership != null && membership.IsActive && membership.FromDate <= now && membership.ToDate >= now,
                TotalSavedAmount = totalSaved,
                TotalRedeemedAmount = totalRedeemed,
                AvailableBalance = availableBalance,
                TotalPaidMonthsCount = entries.Count,
                Entries = entries
            };
        }

        public async Task<bool> RedeemMonthlyPaymentAsync(int paymentId)
        {
            var payment = await _dbContext.MonthlyPayments
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null || payment.IsRedeemed)
            {
                return false; // not found or already redeemed
            }

            payment.IsRedeemed = true;
            payment.RedeemedDate = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}

