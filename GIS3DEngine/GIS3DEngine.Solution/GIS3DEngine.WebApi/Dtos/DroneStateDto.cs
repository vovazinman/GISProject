using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.WebApi.Dtos;

public class DroneStateDto
{
    public string DroneId { get; set; } = string.Empty;

    public DroneStatus Status { get; set; }

    public FlightMode FlightMode { get; set; }

    public Vector3D Position { get; set; }

    public double AltitudeAGL { get; set; }

    public double GroundSpeed { get; set; }

    public double BatteryPercent { get; set; }

    public double DistanceFromHome { get; set; }

    public double DistanceTraveled { get; set; }

    public double FlightTimeSec { get; set; }

    public int CurrentWaypointIndex { get; set; }

    public int TotalWaypoints { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    // 🔁 Mapper from Domain → DTO
    public static DroneStateDto From(DroneState state)
    {
        return new DroneStateDto
        {
            DroneId = state.DroneId,
            Status = state.Status,
            FlightMode = state.FlightMode,
            Position = state.Position,
            AltitudeAGL = state.AltitudeAGL,
            GroundSpeed = state.GroundSpeed,
            BatteryPercent = state.BatteryPercent,
            DistanceFromHome = state.DistanceFromHome,
            DistanceTraveled = state.DistanceTraveled,
            FlightTimeSec = state.FlightTimeSec,
            CurrentWaypointIndex = state.CurrentWaypointIndex,
            TotalWaypoints = state.TotalWaypoints,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
