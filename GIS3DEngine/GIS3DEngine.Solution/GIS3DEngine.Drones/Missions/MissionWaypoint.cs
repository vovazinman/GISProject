using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Waypoint with additional mission parameters.
/// </summary>
public class MissionWaypoint
{
    /// <summary>Position (X, Y for lat/lon mode or local coords).</summary>
    public Vector3D Position { get; set; }

    /// <summary>Altitude at this waypoint (0 = use mission default).</summary>
    public double Altitude { get; set; }

    /// <summary>Speed to this waypoint (0 = use mission default).</summary>
    public double Speed { get; set; }

    /// <summary>Time to hold/hover at waypoint (seconds).</summary>
    public double HoldTimeSec { get; set; } = 0;

    /// <summary>Heading at waypoint (NaN = auto).</summary>
    public double Heading { get; set; } = double.NaN;

    /// <summary>Action to perform at waypoint.</summary>
    public WaypointAction Action { get; set; } = WaypointAction.None;
}

