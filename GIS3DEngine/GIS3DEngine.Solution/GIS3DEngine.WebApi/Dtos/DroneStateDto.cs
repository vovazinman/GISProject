using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.WebApi.Dtos;

public class DroneStateDto
{
    public string DroneId { get; set; } = string.Empty;
    public DroneStatus Status { get; set; }
    public FlightMode FlightMode { get; set; }
    public Vector3D Position { get; set; }
    public Vector3D Velocity { get; set; }
    public double AltitudeAGL { get; set; }
    public double GroundSpeed { get; set; }
    public double BatteryPercent { get; set; }
    public double DistanceFromHome { get; set; }
    public double DistanceTraveled { get; set; }
    public double FlightTimeSec { get; set; }
    public bool IsArmed { get; set; }
    public string? CurrentMissionId { get; set; }
    public int CurrentWaypointIndex { get; set; }
    public int TotalWaypoints { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

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

    //Overload: from DroneState directly (if needed)
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