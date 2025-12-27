using GIS3DEngine.Application.Dtos.Common;

namespace GIS3DEngine.Application.Dtos.Responses;

public record GotoResponseDto
{
    public bool Success { get; init; }
    public string DroneId { get; init; } = string.Empty;
    public Vector3Dto Destination { get; init; } = new();
    public double Distance { get; init; }
    public double ETA { get; init; }
    public string Mode { get; init; } = "Direct";
    public string Message { get; init; } = string.Empty;
}