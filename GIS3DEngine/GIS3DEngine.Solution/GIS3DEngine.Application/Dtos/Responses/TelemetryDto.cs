using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Core.Primitives;

namespace GIS3DEngine.Application.Dtos.Responses;

public record TelemetryDto
{
    public string DroneId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Vector3Dto Position { get; init; } = new();
    public double Altitude { get; init; }
    public double Speed { get; init; }
    public double Battery { get; init; }
    public double Heading { get; init; }
}