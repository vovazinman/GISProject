namespace GIS3DEngine.Application.Dtos.Requests;

public record MissionPlanRequestDto
{
    public string DroneId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}