using Microsoft.AspNetCore.SignalR;

namespace ECommerceSystem.Api.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendOrderUpdate(int orderId, string status)
        {
            await Clients.All.SendAsync("ReceiveOrderUpdate", orderId, status);
        }
    }
}