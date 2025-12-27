using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Application.Dtos.Responses;
using GIS3DEngine.Application.Interfaces;
using GIS3DEngine.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GIS3DEngine.WebApi.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<DroneHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<DroneHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastDroneStateAsync(DroneStateDto state)
    {
        _logger.LogDebug(
            "Broadcasting state for drone {DroneId}: {Status}",
            state.DroneId, state.Status);

        await _hubContext.Clients.All.SendAsync("DroneStateUpdated", state);

        // Also send to drone-specific group
        await _hubContext.Clients
            .Group($"drone-{state.DroneId}")
            .SendAsync("DroneStateUpdated", state);
    }

    public async Task BroadcastFlightPathAsync(FlightPathDto path, double distance, double eta)
    {
        var payload = new
        {
            path.DroneId,
            path.Waypoints,
            path.TotalDistance,
            path.TotalDuration,
            Distance = distance,
            ETA = eta,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug(
            "Broadcasting flight path for drone {DroneId}: Distance={Distance:F0}m, ETA={ETA:F0}s",
            path.DroneId, distance, eta);

        await _hubContext.Clients.All.SendAsync("FlightPathUpdated", payload);

        await _hubContext.Clients
            .Group($"drone-{path.DroneId}")
            .SendAsync("FlightPathUpdated", payload);
    }

    public async Task BroadcastMissionUpdateAsync(
        string droneId,
        string missionId,
        string status,
        double progress)
    {
        var update = new
        {
            DroneId = droneId,
            MissionId = missionId,
            Status = status,
            Progress = progress,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug(
            "Broadcasting mission update: Drone={DroneId}, Mission={MissionId}, Status={Status}, Progress={Progress:P0}",
            droneId, missionId, status, progress);

        await _hubContext.Clients.All.SendAsync("MissionUpdated", update);

        await _hubContext.Clients
            .Group($"drone-{droneId}")
            .SendAsync("MissionUpdated", update);
    }

    public async Task BroadcastAlertAsync(string droneId, string alertType, string message)
    {
        var alert = new
        {
            DroneId = droneId,
            AlertType = alertType,
            Message = message,
            Severity = GetAlertSeverity(alertType),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning(
            "Broadcasting alert: Drone={DroneId}, Type={AlertType}, Message={Message}",
            droneId, alertType, message);

        await _hubContext.Clients.All.SendAsync("AlertReceived", alert);

        await _hubContext.Clients
            .Group($"drone-{droneId}")
            .SendAsync("AlertReceived", alert);
    }

    public async Task BroadcastTelemetryAsync(TelemetryDto telemetry)
    {
        _logger.LogTrace(
            "Broadcasting telemetry for drone {DroneId}",
            telemetry.DroneId);

        // Telemetry only to subscribers (high frequency)
        await _hubContext.Clients
            .Group($"drone-{telemetry.DroneId}")
            .SendAsync("TelemetryUpdated", telemetry);
    }

    private static string GetAlertSeverity(string alertType) => alertType.ToLower() switch
    {
        "emergency" => "critical",
        "battery_critical" => "critical",
        "collision" => "critical",
        "battery_low" => "warning",
        "geofence" => "warning",
        "wind" => "warning",
        "connection_weak" => "warning",
        "connection_lost" => "error",
        "gps_lost" => "error",
        "motor_failure" => "error",
        _ => "info"
    };
}