using GIS3DEngine.Application.Dtos.Common;

namespace GIS3DEngine.Application.Dtos.Responses;

public record FlightStatusDto
{
    public string DroneId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Vector3Dto Position { get; init; } = new();
    public Vector3Dto? Destination { get; init; }
    public double DistanceRemaining { get; init; }
    public double Progress { get; init; }
    public bool IsFlying { get; init; }
}