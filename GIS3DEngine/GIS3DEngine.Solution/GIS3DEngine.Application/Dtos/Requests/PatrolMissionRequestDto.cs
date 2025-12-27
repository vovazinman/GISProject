using GIS3DEngine.Application.Dtos.Common;

namespace GIS3DEngine.Application.Dtos.Requests;

public record PatrolMissionRequestDto
{
    public string DroneId { get; init; } = string.Empty;
    public string? Name { get; init; }
    public IReadOnlyList<PointDto> Points { get; init; } = [];
    public double Altitude { get; init; } = 50;
    public double Speed { get; init; } = 10;
    public int Loops { get; init; } = 1;
}