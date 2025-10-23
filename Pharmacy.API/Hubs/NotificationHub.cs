using Microsoft.AspNetCore.SignalR;
using Pharmacy.Core.Models;

namespace Pharmacy.API.Hubs
{
    public class NotificationHub : Hub
    {
        // Called by a DeliveryMan client after connecting to join his own group
        public Task JoinDeliveryManGroup(int deliveryManId)
        {
            var groupName = $"deliveryman-{deliveryManId}";
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Called by Admin Dashboard clients to join admins group
        public Task JoinAdminGroup()
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }

        public Task LeaveDeliveryManGroup(int deliveryManId)
        {
            var groupName = $"deliveryman-{deliveryManId}";
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveAdminGroup()
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admin");
        }

        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Optional: you can track connection -> user mapping if needed
            return base.OnDisconnectedAsync(exception);
        }
    }
}
