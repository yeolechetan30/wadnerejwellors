using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Hubs
{
    public class GoldRateHub : Hub
    {
        public async Task SendRateUpdate(GoldSilverRateDto rate)
        {
            await Clients.All.SendAsync("ReceiveRateUpdate", rate);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
