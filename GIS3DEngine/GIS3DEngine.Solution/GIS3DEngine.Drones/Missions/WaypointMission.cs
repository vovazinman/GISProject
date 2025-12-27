using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Custom waypoint mission.
/// </summary>
public class WaypointMission : DroneMission
{
    public override MissionType Type => MissionType.Waypoint;

    /// <summary>List of waypoints to visit.</summary>
    public List<MissionWaypoint> Waypoints { get; set; } = new();

    public override FlightPath GenerateFlightPath()
    {
        if (Waypoints.Count == 0)
            throw new InvalidOperationException("No waypoints defined");

        var pathWaypoints = new List<Waypoint>();
        var time = 0.0;

        // Start
        pathWaypoints.Add(new Waypoint(HomePosition, time));
        time += 5;

        var prev = HomePosition + new Vector3D(0, 0, Altitude);

        foreach (var wp in Waypoints)
        {
            var wpPos = new Vector3D(wp.Position.X, wp.Position.Y,
                wp.Altitude > 0 ? HomePosition.Z + wp.Altitude : HomePosition.Z + Altitude);

            var distance = Vector3D.Distance(prev, wpPos);
            var speed = wp.Speed > 0 ? wp.Speed : Speed;
            time += distance / speed;
            time += wp.HoldTimeSec;

            pathWaypoints.Add(new Waypoint(wpPos, time, speed));
            prev = wpPos;
        }

        // Return home
        var returnDist = Vector3D.Distance(pathWaypoints.Last().Position, HomePosition);
        time += returnDist / Speed;
        pathWaypoints.Add(new Waypoint(HomePosition, time));

        EstimatedDurationSec = time;
        EstimatedDistanceM = CalculateTotalDistance(pathWaypoints);

        return FlightPath.CreateSpline(pathWaypoints);
    }

    private static double CalculateTotalDistance(List<Waypoint> waypoints)
    {
        double total = 0;
        for (int i = 1; i < waypoints.Count; i++)
        {
            total += Vector3D.Distance(waypoints[i - 1].Position, waypoints[i].Position);
        }
        return total;
    }
}

