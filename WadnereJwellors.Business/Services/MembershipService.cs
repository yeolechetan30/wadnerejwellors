using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Context;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.Business.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ICashfreePaymentService _paymentService;

        public MembershipService(
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            ICashfreePaymentService paymentService)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _paymentService = paymentService;
        }

        public async Task<MembershipStatusResponseDto> CheckMembershipStatusAsync(int registrationId)
        {
            var now = DateTime.UtcNow;
            var membership = await _dbContext.UserMemberships
                .Where(m => m.RegistrationId == registrationId && m.IsActive)
                .OrderByDescending(m => m.ToDate)
                .FirstOrDefaultAsync();

            if (membership != null && membership.FromDate <= now && membership.ToDate >= now)
            {
                return new MembershipStatusResponseDto
                {
                    HasActiveMembership = true,
                    MembershipCode = membership.MembershipCode,
                    Amount = membership.Amount,
                    FromDate = membership.FromDate,
                    ToDate = membership.ToDate,
                    CanRenew = false,
                    Message = $"Active membership exists until {membership.ToDate:dd-MMM-yyyy}. Renewal not allowed within validity."
                };
            }

            return new MembershipStatusResponseDto
            {
                HasActiveMembership = false,
                CanRenew = true,
                Message = "No active membership found. Eligible to take membership."
            };
        }

        public async Task<PaymentOrderResponseDto> InitiateMembershipOrderAsync(MembershipRequestDto request)
        {
            // 1. Fetch the Customer record from UserRegistration
            var customer = await _dbContext.UserRegistrations
                .FirstOrDefaultAsync(u => u.RegistrationId == request.RegistrationId);

            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with RegistrationId {request.RegistrationId} does not exist.");
            }

            // 2. Validation: Prevent taking membership if already active
            var status = await CheckMembershipStatusAsync(request.RegistrationId);
            if (status.HasActiveMembership && !status.CanRenew)
            {
                throw new InvalidOperationException(status.Message);
            }

            // 3. Read membership fee from request or appsettings.json
            decimal fee = request.Amount ?? _configuration.GetValue<decimal>("MembershipSettings:MembershipFee", 500.00m);

            // 4. Create Cashfree payment order tagged with "PURPOSE_MEMBERSHIP"
            var orderRequest = new CreatePaymentRequestDto
            {
                RegistrationId = customer.RegistrationId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                CustomerEmail = customer.EmailAddress ?? string.Empty,
                CustomerPhone = customer.MobileNumber.ToString(),
                Amount = fee,
                OrderNote = "PURPOSE_MEMBERSHIP"
            };

            return await _paymentService.CreateOrderAsync(orderRequest);
        }

        public async Task<string> ActivateMembershipAsync(int registrationId, int transactionId, decimal amount)
        {
            var now = DateTime.UtcNow;

            // Sequential ID generation: mem_20260001
            int currentYear = now.Year;
            string prefix = $"mem_{currentYear}";

            int countThisYear = await _dbContext.UserMemberships
                .CountAsync(m => m.MembershipCode.StartsWith(prefix));

            string membershipCode = $"{prefix}{(countThisYear + 1):D4}";

            int validityYears = _configuration.GetValue<int>("MembershipSettings:ValidityInYears", 1);
            var toDate = now.AddYears(validityYears);

            var membership = new UserMembership
            {
                RegistrationId = registrationId,
                MembershipCode = membershipCode,
                Amount = amount,
                FromDate = now,
                ToDate = toDate,
                IsActive = true,
                PaymentTransactionId = transactionId,
                CreatedDate = now
            };

            _dbContext.UserMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            return membershipCode;
        }
    }
}
