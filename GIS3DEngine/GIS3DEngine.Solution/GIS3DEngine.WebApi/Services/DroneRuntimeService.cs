using GIS3DEngine.Drones.Core;
using GIS3DEngine.WebApi.Dtos;
using GIS3DEngine.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GIS3DEngine.WebApi.Services;

public class DroneRuntimeService
{
    private readonly IHubContext<DroneHub> _hub;

    public DroneRuntimeService(IHubContext<DroneHub> hub)
    {
        _hub = hub;
    }

    public async Task UpdateDroneAsync(Drone drone)
    {
        var state = DroneStateDto.From(drone.State);
        await _hub.Clients.All.SendAsync("DroneStateUpdated", state);
    }
}

