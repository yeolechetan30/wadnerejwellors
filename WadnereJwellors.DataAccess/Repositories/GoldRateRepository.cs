using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WadnereJwellors.DataAccess.Context;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.DataAccess.Repositories
{
    public class GoldRateRepository : IGoldRateRepository
    {
        private readonly ApplicationDbContext _context;

        public GoldRateRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GoldSilverRate?> GetLatestRateAsync()
        {
            try
            {
                return await _context.GoldSilverRates
                    .OrderByDescending(r => r.Timestamp)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                // Gracefully handle if table does not exist yet in database
                return null;
            }
        }

        public async Task<IEnumerable<GoldSilverRate>> GetRateHistoryAsync(int count = 50)
        {
            try
            {
                return await _context.GoldSilverRates
                    .OrderByDescending(r => r.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return Enumerable.Empty<GoldSilverRate>();
            }
        }

        public async Task SaveRateAsync(GoldSilverRate rate)
        {
            try
            {
                await _context.GoldSilverRates.AddAsync(rate);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Ignore save errors if database table has not been created yet
            }
        }
    }
}
