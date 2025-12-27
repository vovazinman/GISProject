using System.Text.Json.Serialization;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Core.Animation;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Core.Flights;

namespace GIS3DEngine.Drones.Missions;


/// <summary>
/// Types of drone missions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MissionType
{
    /// <summary>Area survey/mapping mission.</summary>
    Survey,
    /// <summary>Perimeter patrol mission.</summary>
    Patrol,
    /// <summary>Package delivery mission.</summary>
    Delivery,
    /// <summary>Search and rescue grid pattern.</summary>
    SearchAndRescue,
    /// <summary>Point inspection mission.</summary>
    Inspection,
    /// <summary>Orbit/circle around point.</summary>
    Orbit,
    /// <summary>Custom waypoint mission.</summary>
    Waypoint,
    /// <summary>Follow moving target.</summary>
    Follow,
    /// <summary>Photo/video capture mission.</summary>
    Photography,
    /// <summary>Agricultural spraying mission.</summary>
    Cancelled
}








