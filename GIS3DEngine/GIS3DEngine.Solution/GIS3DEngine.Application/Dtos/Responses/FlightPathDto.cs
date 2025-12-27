using GIS3DEngine.Core.Flights;

namespace GIS3DEngine.Application.Dtos.Responses;

/// <summary>
/// Flight path DTO
/// </summary>
public record FlightPathDto
{
    public string DroneId { get; init; } = string.Empty;
    public IReadOnlyList<WaypointDto> Waypoints { get; init; } = [];
    public double TotalDistance { get; init; }
    public double TotalDuration { get; init; }

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