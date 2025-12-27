using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Perimeter patrol mission.
/// </summary>
public class PatrolMission : DroneMission
{
    public override MissionType Type => MissionType.Patrol;
    public List<Vector3D> PatrolPoints { get; set; } = new();

    /// <summary>Patrol path waypoints.</summary>
    public List<Vector3D> PatrolPath { get; set; } = new();

    /// <summary>Number of patrol loops (0 = infinite).</summary>
    public int Loops { get; set; } = 1;

    /// <summary>Pause duration at each waypoint (seconds).</summary>
    public double WaypointPauseSec { get; set; } = 5.0;

    /// <summary>Enable 360° scan at waypoints.</summary>
    public bool ScanAtWaypoints { get; set; } = false;

    /// <summary>Inset distance from perimeter (meters).</summary>
    public double PerimeterInset { get; set; } = 10.0;

    public override FlightPath GenerateFlightPath()
    {
        if (PatrolPath.Count < 2)
            throw new InvalidOperationException("Patrol path must have at least 2 waypoints");

        var waypoints = new List<Waypoint>();
        var time = 0.0;

        // Takeoff
        waypoints.Add(new Waypoint(HomePosition, time));
        time += 5;

        // Fly to first patrol point
        var firstPoint = new Vector3D(PatrolPath[0].X, PatrolPath[0].Y, HomePosition.Z + Altitude);
        var distanceToFirst = Vector3D.Distance(HomePosition, firstPoint); // Updated from PatrolPoints[0] to firstPoint
        time += distanceToFirst / Speed;
        waypoints.Add(new Waypoint(firstPoint, time, Speed));

        var loops = Loops == 0 ? 1 : Loops; // Generate at least one loop
        for (int loop = 0; loop < loops; loop++)
        {
            var prevPoint = waypoints.Last().Position;

            foreach (var point in PatrolPath)
            {
                var patrolPoint = new Vector3D(point.X, point.Y, HomePosition.Z + Altitude);
                var distance = Vector3D.Distance(prevPoint, patrolPoint);
                time += distance / Speed;
                time += WaypointPauseSec;
                waypoints.Add(new Waypoint(patrolPoint, time, Speed));
                prevPoint = patrolPoint;
            }

            // Return to first patrol point if looping
            if (loop < loops - 1 || Loops == 0)
            {
                firstPoint = new Vector3D(PatrolPath[0].X, PatrolPath[0].Y, HomePosition.Z + Altitude);
                var distance = Vector3D.Distance(prevPoint, firstPoint);
                time += distance / Speed;
                waypoints.Add(new Waypoint(firstPoint, time, Speed));
            }
        }

        // Return home
        var returnDistance = Vector3D.Distance(waypoints.Last().Position, HomePosition);
        time += returnDistance / Speed;
        waypoints.Add(new Waypoint(HomePosition, time));

        EstimatedDurationSec = time;
        EstimatedDistanceM = CalculateTotalDistance(waypoints);

        return Loops == 0
            ? FlightPath.CreateSpline(waypoints, isLooping: true)
            : FlightPath.CreateSpline(waypoints);
    }

    /// <summary>
    /// Create patrol path from polygon perimeter.
    /// </summary>
    public static PatrolMission FromPolygon(Polygon2D polygon, double altitude = 50, double speed = 10)
    {
        return new PatrolMission
        {
            PatrolPath = polygon.Vertices.ToList(),
            Altitude = altitude,
            Speed = speed
        };
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

