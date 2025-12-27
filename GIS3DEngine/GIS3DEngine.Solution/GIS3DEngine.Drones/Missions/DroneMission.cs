using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Base class for all drone missions.
/// </summary>
public abstract class DroneMission
{
    /// <summary>Unique mission identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Mission name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mission description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Mission type.</summary>
    public abstract MissionType Type { get; }

    /// <summary>Current mission status.</summary>
    public MissionStatus Status { get; set; } = MissionStatus.Created;

    /// <summary>Assigned drone ID.</summary>
    public string? AssignedDroneId { get; set; }

    /// <summary>Priority (1-10, higher = more important).</summary>
    public int Priority { get; set; } = 5;

    /// <summary>Flight altitude in meters AGL.</summary>
    public double Altitude { get; set; } = 50.0;

    /// <summary>Flight speed in m/s.</summary>
    public double Speed { get; set; } = 10.0;

    /// <summary>Home/launch position.</summary>
    public Vector3D HomePosition { get; set; } = Vector3D.Zero;

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Scheduled start time.</summary>
    public DateTime? ScheduledStart { get; set; }

    /// <summary>Actual start time.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Completion time.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Estimated duration in seconds.</summary>
    public double EstimatedDurationSec { get; set; }

    /// <summary>Estimated distance in meters.</summary>
    public double EstimatedDistanceM { get; set; }

    /// <summary>Safety settings.</summary>
    public MissionSafetySettings Safety { get; set; } = new();

    /// <summary>
    /// Generate the flight path for this mission.
    /// </summary>
    public abstract FlightPath GenerateFlightPath();

    /// <summary>
    /// Validate mission parameters.
    /// </summary>
    public virtual MissionValidationResult Validate()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (Altitude < 5) errors.Add("Altitude must be at least 5 meters");
        if (Altitude > 500) warnings.Add("Altitude exceeds typical regulations (>500m)");
        if (Speed <= 0) errors.Add("Speed must be positive");
        if (Speed > 30) warnings.Add("Speed is very high (>30 m/s)");

        return new MissionValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}



