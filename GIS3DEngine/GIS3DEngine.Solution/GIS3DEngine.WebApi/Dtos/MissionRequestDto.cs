using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.WebApi.Dtos;

public class MissionRequestDto
{
    /// <summary>
    /// Natural language description of the mission
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Drone specifications (model, limits, etc.)
    /// </summary>
    public DroneSpecifications Specs { get; set; } = default!;

    /// <summary>
    /// Home position of the drone
    /// </summary>
    public Vector3D HomePosition { get; set; }

    /// <summary>
    /// Current battery percentage (0–100)
    /// </summary>
    public double BatteryPercent { get; set; }
}
