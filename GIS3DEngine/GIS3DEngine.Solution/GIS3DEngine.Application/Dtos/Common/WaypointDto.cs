using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Primitives;

namespace GIS3DEngine.Application.Dtos.Common;

public record WaypointDto
{
    public Vector3D Position { get; init; }
    public double Time { get; init; }
    public double Speed { get; init; }

    public static WaypointDto From(Waypoint wp) => new()
    {
        Position = wp.Position,
        Time = wp.Time,
        Speed = wp.Speed ?? 0
    };
}