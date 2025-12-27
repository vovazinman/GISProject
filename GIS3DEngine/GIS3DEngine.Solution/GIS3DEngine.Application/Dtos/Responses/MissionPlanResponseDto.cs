using GIS3DEngine.Application.Dtos.Common;

namespace GIS3DEngine.Application.Dtos.Responses;

public record MissionPlanResponseDto
{
    public bool Success { get; init; }
    public string MissionId { get; init; } = string.Empty;
    public string MissionType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int WaypointCount { get; init; }
    public double EstimatedDurationMin { get; init; }
    public double EstimatedDistanceM { get; init; }
    public IReadOnlyList<WaypointDto> Waypoints { get; init; } = [];
    public string? ErrorMessage { get; init; }
}