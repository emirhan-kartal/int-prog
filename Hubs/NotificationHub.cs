using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class NotificationHub : Hub
{
    public async Task SendNotification(string user, string message)
    {
        await Clients.User(user).SendAsync("ReceiveNotification", message);
    }
} 