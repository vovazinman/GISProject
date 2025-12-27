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
/// Search and rescue grid search mission.
/// </summary>
public class SearchAndRescueMission : DroneMission
{
    public override MissionType Type => MissionType.SearchAndRescue;

    /// <summary>Search area center point.</summary>
    public Vector3D SearchCenter { get; set; }

    /// <summary>Search area radius in meters.</summary>
    public double SearchRadius { get; set; } = 500.0;

    /// <summary>Grid cell size in meters.</summary>
    public double GridSize { get; set; } = 30.0;

    /// <summary>Expanding square search (true) or grid (false).</summary>
    public bool ExpandingSquare { get; set; } = true;

    /// <summary>Hover time per cell for inspection (seconds).</summary>
    public double CellInspectionSec { get; set; } = 3.0;

    public override FlightPath GenerateFlightPath()
    {
        var waypoints = new List<Waypoint>();
        var time = 0.0;

        // Start
        waypoints.Add(new Waypoint(HomePosition, time));
        time += 5;

        // Go to search center at altitude
        var searchStart = new Vector3D(SearchCenter.X, SearchCenter.Y, HomePosition.Z + Altitude);
        var toCenter = Vector3D.Distance(HomePosition + new Vector3D(0, 0, Altitude), searchStart);
        time += 5 + toCenter / Speed;
        waypoints.Add(new Waypoint(searchStart, time));

        // Generate search pattern
        var searchPoints = ExpandingSquare
            ? GenerateExpandingSquare()
            : GenerateGridSearch();

        foreach (var point in searchPoints)
        {
            var searchPoint = new Vector3D(point.X, point.Y, HomePosition.Z + Altitude);
            var prev = waypoints.Last().Position;
            var distance = Vector3D.Distance(prev, searchPoint);
            time += distance / Speed;
            time += CellInspectionSec;
            waypoints.Add(new Waypoint(searchPoint, time, Speed));
        }

        // Return home
        var returnDist = Vector3D.Distance(waypoints.Last().Position, HomePosition);
        time += returnDist / Speed;
        waypoints.Add(new Waypoint(HomePosition, time));

        EstimatedDurationSec = time;
        EstimatedDistanceM = CalculateTotalDistance(waypoints);

        return FlightPath.CreateSpline(waypoints);
    }

    private List<Vector3D> GenerateExpandingSquare()
    {
        var points = new List<Vector3D> { SearchCenter };
        var x = SearchCenter.X;
        var y = SearchCenter.Y;

        // Expanding square pattern
        int direction = 0; // 0=E, 1=N, 2=W, 3=S
        int steps = 1;
        int stepCount = 0;
        int turnCount = 0;

        while (Vector3D.Distance(new Vector3D(x, y, 0), SearchCenter) < SearchRadius)
        {
            switch (direction)
            {
                case 0: x += GridSize; break;
                case 1: y += GridSize; break;
                case 2: x -= GridSize; break;
                case 3: y -= GridSize; break;
            }

            points.Add(new Vector3D(x, y, 0));
            stepCount++;

            if (stepCount >= steps)
            {
                stepCount = 0;
                direction = (direction + 1) % 4;
                turnCount++;
                if (turnCount % 2 == 0) steps++;
            }
        }

        return points;
    }

    private List<Vector3D> GenerateGridSearch()
    {
        var points = new List<Vector3D>();
        var startX = SearchCenter.X - SearchRadius;
        var startY = SearchCenter.Y - SearchRadius;
        var rows = (int)(2 * SearchRadius / GridSize);

        for (int row = 0; row <= rows; row++)
        {
            var y = startY + row * GridSize;
            var isReverse = row % 2 == 1;

            if (isReverse)
            {
                for (double x = SearchCenter.X + SearchRadius; x >= startX; x -= GridSize)
                {
                    var dist = Vector3D.Distance(new Vector3D(x, y, 0), SearchCenter);
                    if (dist <= SearchRadius)
                        points.Add(new Vector3D(x, y, 0));
                }
            }
            else
            {
                for (double x = startX; x <= SearchCenter.X + SearchRadius; x += GridSize)
                {
                    var dist = Vector3D.Distance(new Vector3D(x, y, 0), SearchCenter);
                    if (dist <= SearchRadius)
                        points.Add(new Vector3D(x, y, 0));
                }
            }
        }

        return points;
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

