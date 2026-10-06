using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.Business.Services;
using WadnereJwellors.Models;

namespace WadnereJwellors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoldSilverRatesController : ControllerBase
    {
        private readonly IGoldRateService _goldRateService;

        public GoldSilverRatesController(IGoldRateService goldRateService)
        {
            _goldRateService = goldRateService;
        }

        /// <summary>
        /// Gets the latest live market rates for Gold (24K, 22K, 18K) and Silver.
        /// </summary>
        [HttpGet("latest")]
        [HttpGet("/api/rates/latest")]
        [ProducesResponseType(typeof(GoldSilverRateDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<GoldSilverRateDto>> GetLatestRates()
        {
            var rates = await _goldRateService.GetLatestRatesAsync();
            return Ok(rates);
        }

        /// <summary>
        /// Gets historical live rate logs for trend graphs and charts.
        /// </summary>
        /// <param name="count">Number of historical rate entries to retrieve (default: 50)</param>
        [HttpGet("history")]
        [HttpGet("/api/rates/history")]
        [ProducesResponseType(typeof(IEnumerable<GoldSilverRateDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GoldSilverRateDto>>> GetRateHistory([FromQuery] int count = 50)
        {
            var history = await _goldRateService.GetRateHistoryAsync(count);
            return Ok(history);
        }

        /// <summary>
        /// Manually overrides Gold and Silver rates (Admin function).
        /// </summary>
        /// <param name="request">New rate values</param>
        [HttpPost("manual-override")]
        [HttpPost("/api/rates/manual-override")]
        [ProducesResponseType(typeof(GoldSilverRateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GoldSilverRateDto>> UpdateManualRates([FromBody] ManualRateUpdateRequestDto request)
        {
            var updatedRate = await _goldRateService.UpdateManualRatesAsync(request);
            return Ok(updatedRate);
        }
    }
}
