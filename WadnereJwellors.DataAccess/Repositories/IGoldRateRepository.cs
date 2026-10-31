using System.Collections.Generic;
using System.Threading.Tasks;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.DataAccess.Repositories
{
    public interface IGoldRateRepository
    {
        Task<GoldSilverRate?> GetLatestRateAsync();
        Task<IEnumerable<GoldSilverRate>> GetRateHistoryAsync(int count = 50);
        Task SaveRateAsync(GoldSilverRate rate);
    }
}
