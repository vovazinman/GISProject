using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Actions that can be performed at waypoints.
/// </summary>
public enum WaypointAction
{
    None,
    TakePhoto,
    StartVideo,
    StopVideo,
    RotateGimbal,
    Hover,
    Land,
    RTL
}

