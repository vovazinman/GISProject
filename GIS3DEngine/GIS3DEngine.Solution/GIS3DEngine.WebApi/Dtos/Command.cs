using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Missions;

namespace GIS3DEngine.WebApi.Dtos;

/// <summary>
/// Command response
/// </summary>
public class CommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DroneStateDto? NewState { get; set; }
}

/// <summary>
/// Flight path DTO
/// </summary>
public class FlightPathDto
{
    public string DroneId { get; set; } = string.Empty;
    public List<WaypointDto> Waypoints { get; set; } = new();
    public double TotalDistance { get; set; }
    public double TotalDuration { get; set; }

    public static FlightPathDto From(string droneId, FlightPath path)
    {
        return new FlightPathDto
        {
            DroneId = droneId,
            Waypoints = path.Waypoints.Select(WaypointDto.From).ToList(),
            TotalDistance = path.TotalDistance,
            TotalDuration = path.TotalDuration
        };
    }
}

/// <summary>
/// Waypoint DTO
/// </summary>
public class WaypointDto
{
    public Vector3D Position { get; set; }
    public double Time { get; set; }
    public double Speed { get; set; }

    public static WaypointDto From(Waypoint wp)
    {
        return new WaypointDto
        {
            Position = wp.Position,
            Time = wp.Time,
            Speed = wp.Speed?? 0
        };
    }
}

/// <summary>
/// API error response
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int StatusCode { get; set; }
}

/// <summary>
/// Create drone request
/// </summary>
public class CreateDroneRequest
{
    public string? Id { get; set; }
    public string? SpecsType { get; set; }  // "mavic3", "phantom4", "matrice300"
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

/// <summary>
/// Go to request
/// </summary>
public class GoToRequest
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Speed { get; set; } = 10;
}

/// <summary>
/// Chat request
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? DroneId { get; set; }
}

/// <summary>
/// Chat response
/// </summary>
public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public bool HasCommand { get; set; }
    public string? CommandType { get; set; }
    public bool CommandExecuted { get; set; }
    public string? CommandResult { get; set; }
}

/// <summary>
/// Mission plan request
/// </summary>
public class MissionPlanRequest
{
    public string DroneId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Mission plan response
/// </summary>
public class MissionPlanResponse
{
    public bool Success { get; set; }
    public string MissionId { get; set; } = string.Empty;
    public string MissionType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int WaypointCount { get; set; }
    public double EstimatedDurationMin { get; set; }
    public double EstimatedDistanceM { get; set; }
    public List<WaypointDto> Waypoints { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mission info DTO
/// </summary>
public class MissionInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Altitude { get; set; }
    public double Speed { get; set; }
    public double EstimatedDurationSec { get; set; }
    public double EstimatedDistanceM { get; set; }
    public DateTime CreatedAt { get; set; }

    public static MissionInfoDto From(DroneMission mission)
    {
        return new MissionInfoDto
        {
            Id = mission.Id,
            Name = mission.Name,
            Type = mission.Type.ToString(),
            Status = mission.Status.ToString(),
            Altitude = mission.Altitude,
            Speed = mission.Speed,
            EstimatedDurationSec = mission.EstimatedDurationSec,
            EstimatedDistanceM = mission.EstimatedDistanceM,
            CreatedAt = mission.CreatedAt
        };
    }
}

/// <summary>
/// Survey mission request
/// </summary>
public class SurveyMissionRequest
{
    public string DroneId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public double OriginX { get; set; } = 0;
    public double OriginY { get; set; } = 0;
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 100;
    public double Altitude { get; set; } = 50;
    public double Speed { get; set; } = 10;
    public string Pattern { get; set; } = "Lawnmower";  // Lawnmower, Spiral, Grid
    public double LineSpacing { get; set; } = 20;
}

/// <summary>
/// Orbit mission request
/// </summary>
public class OrbitMissionRequest
{
    public string DroneId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; } = 50;
    public double Altitude { get; set; } = 50;
    public double Speed { get; set; } = 10;
    public int Orbits { get; set; } = 1;
    public bool Clockwise { get; set; } = true;
}

/// <summary>
/// Patrol mission request
/// </summary>
public class PatrolMissionRequest
{
    public string DroneId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public List<PointDto> Points { get; set; } = new();
    public double Altitude { get; set; } = 50;
    public double Speed { get; set; } = 10;
    public int Loops { get; set; } = 1;
}

/// <summary>
/// Delivery mission request
/// </summary>
public class DeliveryMissionRequest
{
    public string DroneId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public double PickupX { get; set; }
    public double PickupY { get; set; }
    public double DeliveryX { get; set; }
    public double DeliveryY { get; set; }
    public double Altitude { get; set; } = 50;
    public double Speed { get; set; } = 10;
    public double HoverTimeAtPickup { get; set; } = 10;
    public double HoverTimeAtDelivery { get; set; } = 10;
}

/// <summary>
/// Simple point DTO
/// </summary>
public class PointDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Telemetry data point
/// </summary>
public class TelemetryDto
{
    public string DroneId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public PositionDto Position { get; set; } = new();
    public double Altitude { get; set; }
    public double Speed { get; set; }
    public double Battery { get; set; }
    public double Heading { get; set; }
}