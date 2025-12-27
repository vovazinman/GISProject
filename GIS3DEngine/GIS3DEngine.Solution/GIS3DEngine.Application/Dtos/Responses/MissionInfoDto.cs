using GIS3DEngine.Drones.Missions;

namespace GIS3DEngine.Application.Dtos.Responses;

public record MissionInfoDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Altitude { get; init; }
    public double Speed { get; init; }
    public double EstimatedDurationSec { get; init; }
    public double EstimatedDistanceM { get; init; }
    public DateTime CreatedAt { get; init; }

    public static MissionInfoDto From(DroneMission mission) => new()
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