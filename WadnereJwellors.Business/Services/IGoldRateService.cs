using System.Collections.Generic;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Services
{
    public interface IGoldRateService
    {
        Task<GoldSilverRateDto> GetLatestRatesAsync();
        Task<IEnumerable<GoldSilverRateDto>> GetRateHistoryAsync(int count = 50);
        Task<GoldSilverRateDto> FetchAndBroadcastLiveRatesAsync();
        Task<GoldSilverRateDto> UpdateManualRatesAsync(ManualRateUpdateRequestDto request);
    }
}
