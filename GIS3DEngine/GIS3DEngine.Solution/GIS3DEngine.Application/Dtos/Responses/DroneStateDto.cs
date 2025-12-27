using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.Application.Dtos.Responses;

public record DroneStateDto
{
    public string DroneId { get; init; } = string.Empty;
    public DroneStatus Status { get; init; }
    public FlightMode FlightMode { get; init; }
    public Vector3D Position { get; init; }
    public Vector3D Velocity { get; init; }
    public double AltitudeAGL { get; init; }
    public double GroundSpeed { get; init; }
    public double BatteryPercent { get; init; }
    public double DistanceFromHome { get; init; }
    public double DistanceTraveled { get; init; }
    public double FlightTimeSec { get; init; }
    public bool IsArmed { get; init; }
    public string? CurrentMissionId { get; init; }
    public int CurrentWaypointIndex { get; init; }
    public int TotalWaypoints { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    // 🔁 Mapper from Domain → DTO
    public static DroneStateDto From(Drone drone)
    {
        var state = drone.State;
        return new DroneStateDto
        {
            DroneId = drone.Id,
            Status = state.Status,
            FlightMode = state.FlightMode,
            Position = state.Position,
            Velocity = state.Velocity,
            AltitudeAGL = state.AltitudeAGL,
            GroundSpeed = state.GroundSpeed,
            BatteryPercent = state.BatteryPercent,
            DistanceFromHome = state.DistanceFromHome,
            DistanceTraveled = state.DistanceTraveled,
            FlightTimeSec = state.FlightTimeSec,
            IsArmed = state.IsArmed,
            CurrentMissionId = drone.CurrentMissionId,
            CurrentWaypointIndex = state.CurrentWaypointIndex,
            TotalWaypoints = state.TotalWaypoints,
            TimestampUtc = DateTime.UtcNow
        };
    }

    // Overload: from DroneState directly
    public static DroneStateDto From(DroneState state, string? missionId = null)
    {
        return new DroneStateDto
        {
            DroneId = state.DroneId,
            Status = state.Status,
            FlightMode = state.FlightMode,
            Position = state.Position,
            Velocity = state.Velocity,
            AltitudeAGL = state.AltitudeAGL,
            GroundSpeed = state.GroundSpeed,
            BatteryPercent = state.BatteryPercent,
            DistanceFromHome = state.DistanceFromHome,
            DistanceTraveled = state.DistanceTraveled,
            FlightTimeSec = state.FlightTimeSec,
            IsArmed = state.IsArmed,
            CurrentMissionId = missionId,
            CurrentWaypointIndex = state.CurrentWaypointIndex,
            TotalWaypoints = state.TotalWaypoints,
            TimestampUtc = DateTime.UtcNow
        };
    }
}