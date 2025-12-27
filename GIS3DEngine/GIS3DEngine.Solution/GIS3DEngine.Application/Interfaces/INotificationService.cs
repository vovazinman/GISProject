using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Application.Dtos.Responses;

namespace GIS3DEngine.Application.Interfaces;

public interface INotificationService
{
    Task BroadcastDroneStateAsync(DroneStateDto state);
    Task BroadcastFlightPathAsync(FlightPathDto path, double distance, double eta);
    Task BroadcastMissionUpdateAsync(string droneId, string missionId, string status, double progress);
    Task BroadcastAlertAsync(string droneId, string alertType, string message);
    Task BroadcastTelemetryAsync(TelemetryDto telemetry);
}