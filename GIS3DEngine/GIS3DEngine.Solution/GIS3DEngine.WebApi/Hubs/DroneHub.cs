using GIS3DEngine.WebApi.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace GIS3DEngine.WebApi.Hubs;

/// <summary>
/// SignalR Hub for real-time drone updates
/// </summary>
public class DroneHub : Hub
{
    private readonly ILogger<DroneHub> _logger;

    public DroneHub(ILogger<DroneHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Broadcast drone state to all clients
    /// </summary>
    public async Task BroadcastDroneState(DroneStateDto state)
    {
        await Clients.All.SendAsync("DroneStateUpdated", state);
    }

    /// <summary>
    /// Send drone state to specific drone group
    /// </summary>
    public async Task SendDroneState(string droneId, DroneStateDto state)
    {
        await Clients.Group(droneId).SendAsync("DroneStateUpdated", state);
    }

    /// <summary>
    /// Broadcast telemetry to all clients
    /// </summary>
    public async Task BroadcastTelemetry(TelemetryDto telemetry)
    {
        await Clients.All.SendAsync("TelemetryReceived", telemetry);
    }

    /// <summary>
    /// Send flight path update
    /// </summary>
    public async Task SendFlightPath(FlightPathDto path)
    {
        await Clients.All.SendAsync("FlightPathUpdated", path);
    }

    /// <summary>
    /// Send chat message
    /// </summary>
    public async Task SendChatMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    /// <summary>
    /// Stream chat response (for AI streaming)
    /// </summary>
    public async Task StreamChatChunk(string chunk)
    {
        await Clients.Caller.SendAsync("ChatChunk", chunk);
    }

    /// <summary>
    /// Send mission update
    /// </summary>
    public async Task SendMissionUpdate(string missionId, string status, double progress)
    {
        await Clients.All.SendAsync("MissionUpdated", new { missionId, status, progress });
    }

    /// <summary>
    /// Send alert/warning
    /// </summary>
    public async Task SendAlert(string droneId, string alertType, string message)
    {
        await Clients.All.SendAsync("AlertReceived", new { droneId, alertType, message });
    }

    /// <summary>
    /// Client subscribes to specific drone updates
    /// </summary>
    public async Task SubscribeToDrone(string droneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, droneId);
        _logger.LogInformation("Client {ConnectionId} subscribed to drone {DroneId}", Context.ConnectionId, droneId);
    }

    /// <summary>
    /// Client unsubscribes from drone updates
    /// </summary>
    public async Task UnsubscribeFromDrone(string droneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, droneId);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from drone {DroneId}", Context.ConnectionId, droneId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
