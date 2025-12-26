using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.WebApi.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace GIS3DEngine.WebApi.Hubs;

/// <summary>
/// SignalR Hub for real-time drone updates
/// </summary>
public class DroneHub : Hub
{
    private readonly DroneFleetManager _fleet;
    private readonly ILogger<DroneHub> _logger;

    public DroneHub(DroneFleetManager fleet, ILogger<DroneHub> logger)
    {
        _fleet = fleet;
        _logger = logger;
    }

    // ========== Connection Events ==========

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("═══════════════════════════════════════");
        _logger.LogInformation("CLIENT CONNECTED");
        _logger.LogInformation("ConnectionId: {ConnectionId}", Context.ConnectionId);
        _logger.LogInformation("Time: {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
        _logger.LogInformation("═══════════════════════════════════════");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogWarning("═══════════════════════════════════════");
        _logger.LogWarning("CLIENT DISCONNECTED");
        _logger.LogWarning("ConnectionId: {ConnectionId}", Context.ConnectionId);
        _logger.LogWarning("Reason: {Reason}", exception?.Message ?? "Normal");
        _logger.LogWarning("═══════════════════════════════════════");

        await base.OnDisconnectedAsync(exception);
    }

    // ========== Subscription Methods ==========

    /// <summary>
    /// Client subscribes to specific drone updates
    /// </summary>
    public async Task SubscribeToDrone(string droneId)
    {
        _logger.LogInformation("═══════════════════════════════════════");
        _logger.LogInformation("SUBSCRIBE TO DRONE");
        _logger.LogInformation("DroneId: {DroneId}", droneId);
        _logger.LogInformation("ConnectionId: {ConnectionId}", Context.ConnectionId);

        // Add to group
        await Groups.AddToGroupAsync(Context.ConnectionId, droneId);
        _logger.LogInformation("Added to group: {DroneId}", droneId);

        // Send current state immediately
        var drone = _fleet.GetDrone(droneId);
        if (drone != null)
        {
            var dto = DroneStateDto.From(drone);
            await Clients.Caller.SendAsync("DroneStateUpdated", dto);
            _logger.LogInformation("Sent initial state: Status={Status}, Pos=({X:F1}, {Y:F1}, {Z:F1})",
                dto.Status, dto.Position.X, dto.Position.Y, dto.Position.Z);
        }
        else
        {
            _logger.LogWarning("Drone not found: {DroneId}", droneId);
        }

        _logger.LogInformation("═══════════════════════════════════════");
    }

    /// <summary>
    /// Client unsubscribes from drone updates
    /// </summary>
    public async Task UnsubscribeFromDrone(string droneId)
    {
        _logger.LogInformation("UNSUBSCRIBE: {DroneId} | Client: {ConnectionId}", droneId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, droneId);
    }

    /// <summary>
    /// Client requests current drone state
    /// </summary>
    public async Task RequestDroneState(string droneId)
    {
        _logger.LogInformation("STATE REQUEST: {DroneId}", droneId);

        var drone = _fleet.GetDrone(droneId);
        if (drone != null)
        {
            var dto = DroneStateDto.From(drone);
            await Clients.Caller.SendAsync("DroneStateUpdated", dto);
            _logger.LogInformation("   State sent");
        }
        else
        {
            _logger.LogWarning("   ⚠️ Drone not found");
        }
    }

    // ========== Broadcast Methods ==========

    /// <summary>
    /// Broadcast drone state to all clients
    /// </summary>
    public async Task BroadcastDroneState(DroneStateDto state)
    {
        _logger.LogDebug("BROADCAST: {DroneId} | {Status}", state.DroneId, state.Status);
        await Clients.All.SendAsync("DroneStateUpdated", state);
    }

    /// <summary>
    /// Send drone state to specific drone group
    /// </summary>
    public async Task SendDroneState(string droneId, DroneStateDto state)
    {
        _logger.LogDebug("SEND TO GROUP: {DroneId}", droneId);
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
        _logger.LogInformation("FLIGHT PATH: {DroneId} | {Count} waypoints",
            path.DroneId, path.Waypoints?.Count ?? 0);
        await Clients.All.SendAsync("FlightPathUpdated", path);
    }

    /// <summary>
    /// Send chat message
    /// </summary>
    public async Task SendChatMessage(string user, string message)
    {
        _logger.LogInformation("CHAT: {User}: {Message}", user, message);
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
        _logger.LogInformation("MISSION: {MissionId} | {Status} | {Progress:P0}",
            missionId, status, progress);
        await Clients.All.SendAsync("MissionUpdated", new { missionId, status, progress });
    }

    /// <summary>
    /// Send alert/warning
    /// </summary>
    public async Task SendAlert(string droneId, string alertType, string message)
    {
        _logger.LogWarning("ALERT: {DroneId} | {Type} | {Message}", droneId, alertType, message);
        await Clients.All.SendAsync("AlertReceived", new { droneId, alertType, message });
    }
}