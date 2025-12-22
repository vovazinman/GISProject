using GIS3DEngine.WebApi.Dtos;
using Microsoft.AspNetCore.SignalR;


namespace GIS3DEngine.WebApi.Hub;

public class DroneHub : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task BroadcastDroneState(DroneStateDto state)
    {
        await Clients.All.SendAsync("DroneStateUpdated", state);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinDroneGroup(string droneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, droneId);
    }

    public async Task SendToDrone(string droneId, DroneStateDto state)
    {
        await Clients.Group(droneId).SendAsync("DroneStateUpdated", state);
    }
}

