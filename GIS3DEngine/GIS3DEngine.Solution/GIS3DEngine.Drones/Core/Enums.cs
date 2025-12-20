using System.Text.Json.Serialization;

namespace GIS3DEngine.Drones.Core;

//// <summary>
/// Types of drones supported by the system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DroneType
{
    Quadcopter,
    Hexacopter,
    Octocopter,
    FixedWing,
    VTOL,
    Hybrid
}

/// <summary>
/// Drone operational status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DroneStatus
{
    Offline,
    Initializing,
    Ready,
    Armed,
    TakingOff,
    Flying,
    Hovering,
    Landing,
    Landed,
    Returning,
    Emergency,
    LowBattery,
    Crashed,
    MaintenanceRequired
}

/// <summary>
/// Flight modes available.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FlightMode
{
    Manual,
    Stabilized,
    AltitudeHold,
    PositionHold,
    Loiter,
    Auto,
    Guided,
    ReturnToLaunch,
    Land,
    Takeoff,
    Circle,
    Follow
}

/// <summary>
/// Types of sensors a drone can have.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SensorType
{
    GPS,
    IMU,
    Barometer,
    Magnetometer,
    Rangefinder,
    LiDAR,
    Camera,
    ThermalCamera,
    Multispectral,
    Ultrasonic,
    OpticalFlow
}

/// <summary>
/// Emergency event types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmergencyType
{
    LowBattery,
    SignalLost,
    GPSFailure,
    MotorFailure,
    CollisionWarning,
    GeofenceViolation,
    UserTriggered,
    SystemError
}

#region Mission Enums

/// <summary>
/// Types of drone missions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MissionType
{
    Survey,
    Patrol,
    Delivery,
    SearchAndRescue,
    Inspection,
    Orbit,
    Waypoint,
    Follow,
    Photography
}
#endregion

#region Telemetry Enums

/// <summary>
/// Alert severity levels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AlertLevel
{
    Info,
    Warning,
    Critical
}

#endregion