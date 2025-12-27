using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Mission safety settings.
/// </summary>
public class MissionSafetySettings
{
    /// <summary>Minimum battery to start mission (%).</summary>
    public double MinBatteryStart { get; set; } = 50.0;

    /// <summary>Return to home battery threshold (%).</summary>
    public double ReturnBatteryThreshold { get; set; } = 30.0;

    /// <summary>Maximum wind speed to fly (m/s).</summary>
    public double MaxWindSpeed { get; set; } = 10.0;

    /// <summary>Require GPS fix quality.</summary>
    public int MinGpsQuality { get; set; } = 3;

    /// <summary>Enable collision avoidance.</summary>
    public bool CollisionAvoidance { get; set; } = true;

    /// <summary>Geofence radius in meters (0 = no limit).</summary>
    public double GeofenceRadius { get; set; } = 0;

    /// <summary>Geofence center point.</summary>
    public Vector3D? GeofenceCenter { get; set; }
}

