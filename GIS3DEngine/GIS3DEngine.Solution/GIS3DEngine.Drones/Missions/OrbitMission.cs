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
/// Orbit/circle mission around a point of interest.
/// </summary>
public class OrbitMission : DroneMission
{
    public override MissionType Type => MissionType.Orbit;

    /// <summary>Center point to orbit around.</summary>
    public Vector3D OrbitCenter { get; set; }

    /// <summary>Orbit radius in meters.</summary>
    public double OrbitRadius { get; set; } = 50.0;

    /// <summary>Number of orbits (0 = continuous).</summary>
    public int Orbits { get; set; } = 1;

    /// <summary>Orbit clockwise (true) or counter-clockwise (false).</summary>
    public bool Clockwise { get; set; } = true;

    /// <summary>Points per orbit (smoothness).</summary>
    public int PointsPerOrbit { get; set; } = 36;

    /// <summary>Keep camera/heading pointed at center.</summary>
    public bool HeadingToCenter { get; set; } = true;

    public override FlightPath GenerateFlightPath()
    {
        var waypoints = new List<Waypoint>();
        var time = 0.0;

        // Start
        waypoints.Add(new Waypoint(HomePosition, time));
        time += 5;

        // Go to orbit start point
        var orbitAltitude = HomePosition.Z + Altitude;
        var startAngle = 0.0;
        var startPoint = new Vector3D(
            OrbitCenter.X + OrbitRadius * Math.Cos(startAngle),
            OrbitCenter.Y + OrbitRadius * Math.Sin(startAngle),
            orbitAltitude);

        var toStart = Vector3D.Distance(HomePosition + new Vector3D(0, 0, Altitude), startPoint);
        time += 5 + toStart / Speed;
        waypoints.Add(new Waypoint(startPoint, time));

        // Calculate time per segment
        var circumference = 2 * Math.PI * OrbitRadius;
        var orbitTime = circumference / Speed;
        var segmentTime = orbitTime / PointsPerOrbit;

        // Generate orbit points
        var numOrbits = Orbits == 0 ? 1 : Orbits;
        var direction = Clockwise ? 1 : -1;

        for (int orbit = 0; orbit < numOrbits; orbit++)
        {
            for (int i = 1; i <= PointsPerOrbit; i++)
            {
                var angle = startAngle + direction * 2 * Math.PI * i / PointsPerOrbit;
                var point = new Vector3D(
                    OrbitCenter.X + OrbitRadius * Math.Cos(angle),
                    OrbitCenter.Y + OrbitRadius * Math.Sin(angle),
                    orbitAltitude);

                time += segmentTime;
                waypoints.Add(new Waypoint(point, time, Speed));
            }
        }

        // Return home
        var returnDist = Vector3D.Distance(waypoints.Last().Position, HomePosition);
        time += returnDist / Speed;
        waypoints.Add(new Waypoint(HomePosition, time));

        EstimatedDurationSec = time;
        EstimatedDistanceM = circumference * numOrbits + toStart + returnDist;

        return Orbits == 0
            ? FlightPath.CreateSpline(waypoints, isLooping: true)
            : FlightPath.CreateSpline(waypoints);
    }
}

