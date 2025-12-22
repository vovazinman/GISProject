using System.Text.Json.Serialization;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Core.Animation;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.Drones.Missions;

#region Mission Types

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
    Photography
}

/// <summary>
/// Mission status tracking.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MissionStatus
{
    Created,
    Validated,
    Assigned,
    InProgress,
    Paused,
    Completed,
    Aborted,
    Failed
}

/// <summary>
/// Survey pattern types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SurveyPattern
{
    /// <summary>Back and forth parallel lines.</summary>
    Lawnmower,
    /// <summary>Spiral from outside to center.</summary>
    SpiralInward,
    /// <summary>Spiral from center outward.</summary>
    SpiralOutward,
    /// <summary>Grid pattern (crosshatch).</summary>
    Grid,
    /// <summary>Circular orbits.</summary>
    Circular
}

#endregion

#region Mission Base

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

/// <summary>
/// Mission validation result.
/// </summary>
public class MissionValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

#endregion

#region Survey Mission

/// <summary>
/// Area survey/mapping mission.
/// </summary>
public class SurveyMission : DroneMission
{
    public override MissionType Type => MissionType.Survey;

    /// <summary>Survey area polygon vertices.</summary>
    public List<Vector3D> AreaVertices { get; set; } = new();

    /// <summary>Survey pattern to use.</summary>
    public SurveyPattern Pattern { get; set; } = SurveyPattern.Lawnmower;

    /// <summary>Line spacing in meters (overlap control).</summary>
    public double LineSpacing { get; set; } = 20.0;

    /// <summary>Camera overlap percentage (0-100).</summary>
    public double OverlapPercent { get; set; } = 70.0;

    /// <summary>Camera sidelap percentage (0-100).</summary>
    public double SidelapPercent { get; set; } = 60.0;

    /// <summary>Angle of survey lines in degrees (0 = North).</summary>
    public double SurveyAngleDeg { get; set; } = 0;

    /// <summary>Take photos at waypoints.</summary>
    public bool CapturePhotos { get; set; } = true;

    /// <summary>Photo interval in seconds (0 = at waypoints only).</summary>
    public double PhotoIntervalSec { get; set; } = 2.0;

    public override FlightPath GenerateFlightPath()
    {
        if (AreaVertices.Count < 3)
            throw new InvalidOperationException("Survey area must have at least 3 vertices");

        var polygon = Polygon2D.FromVertices(AreaVertices);
        var waypoints = new List<Waypoint>();

        // Start from home
        var time = 0.0;
        waypoints.Add(new Waypoint(HomePosition, time));
        time += 5; // Takeoff time

        // Get bounding box
        var bounds = polygon.Bounds;

        // Generate survey lines based on pattern
        var surveyPoints = Pattern switch
        {
            SurveyPattern.Lawnmower => GenerateLawnmowerPattern(bounds, polygon),
            SurveyPattern.SpiralInward => GenerateSpiralPattern(polygon, inward: true),
            SurveyPattern.SpiralOutward => GenerateSpiralPattern(polygon, inward: false),
            SurveyPattern.Grid => GenerateGridPattern(bounds, polygon),
            SurveyPattern.Circular => GenerateCircularPattern(polygon),
            _ => GenerateLawnmowerPattern(bounds, polygon)
        };

        // Add survey waypoints
        var prevPoint = HomePosition;
        foreach (var point in surveyPoints)
        {
            var surveyPoint = new Vector3D(point.X, point.Y, HomePosition.Z + Altitude);
            var distance = Vector3D.Distance(prevPoint, surveyPoint);
            time += distance / Speed;
            waypoints.Add(new Waypoint(surveyPoint, time, Speed));
            prevPoint = surveyPoint;
        }

        // Return home
        var returnDistance = Vector3D.Distance(prevPoint, HomePosition);
        time += returnDistance / Speed;
        waypoints.Add(new Waypoint(HomePosition, time));

        EstimatedDurationSec = time;
        EstimatedDistanceM = CalculateTotalDistance(waypoints);

        return FlightPath.CreateSpline(waypoints);
    }

    private List<Vector3D> GenerateLawnmowerPattern(BoundingBox bounds, Polygon2D polygon)
    {
        var points = new List<Vector3D>();
        var angleRad = SurveyAngleDeg * Math.PI / 180;
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);

        var width = bounds.Max.X - bounds.Min.X;
        var height = bounds.Max.Y - bounds.Min.Y;
        var center = new Vector3D((bounds.Min.X + bounds.Max.X) / 2, (bounds.Min.Y + bounds.Max.Y) / 2, 0);

        var numLines = (int)Math.Ceiling(width / LineSpacing) + 1;
        var lineLength = height * 1.1;

        for (int i = 0; i < numLines; i++)
        {
            var xOffset = bounds.Min.X + i * LineSpacing - center.X;
            var y1 = -lineLength / 2;
            var y2 = lineLength / 2;

            // Rotate and translate
            var startX = center.X + xOffset * cos - y1 * sin;
            var startY = center.Y + xOffset * sin + y1 * cos;
            var endX = center.X + xOffset * cos - y2 * sin;
            var endY = center.Y + xOffset * sin + y2 * cos;

            var start = new Vector3D(startX, startY, 0);
            var end = new Vector3D(endX, endY, 0);

            // Clip to polygon
            if (polygon.ContainsPoint(start) || polygon.ContainsPoint(end))
            {
                if (i % 2 == 0)
                {
                    points.Add(start);
                    points.Add(end);
                }
                else
                {
                    points.Add(end);
                    points.Add(start);
                }
            }
        }

        return points;
    }

    private List<Vector3D> GenerateSpiralPattern(Polygon2D polygon, bool inward)
    {
        var points = new List<Vector3D>();
        var center = polygon.Centroid;
        var maxRadius = Math.Max(polygon.Bounds.Size.X, polygon.Bounds.Size.Y) / 2;
        var turns = maxRadius / LineSpacing;

        for (double t = 0; t <= turns * 2 * Math.PI; t += 0.1)
        {
            double r = inward ? maxRadius * (1 - t / (turns * 2 * Math.PI)) : maxRadius * t / (turns * 2 * Math.PI);
            var x = center.X + r * Math.Cos(t);
            var y = center.Y + r * Math.Sin(t);
            var point = new Vector3D(x, y, 0);

            if (polygon.ContainsPoint(point))
                points.Add(point);
        }

        return inward ? points : points.AsEnumerable().Reverse().ToList();
    }

    private List<Vector3D> GenerateGridPattern(BoundingBox bounds, Polygon2D polygon)
    {
        var points = new List<Vector3D>();

        // Horizontal lines
        for (double y = bounds.Min.Y; y <= bounds.Max.Y; y += LineSpacing)
        {
            var isReverse = ((y - bounds.Min.Y) / LineSpacing) % 2 == 1;
            if (isReverse)
            {
                for (double x = bounds.Max.X; x >= bounds.Min.X; x -= LineSpacing / 2)
                {
                    var p = new Vector3D(x, y, 0);
                    if (polygon.ContainsPoint(p)) points.Add(p);
                }
            }
            else
            {
                for (double x = bounds.Min.X; x <= bounds.Max.X; x += LineSpacing / 2)
                {
                    var p = new Vector3D(x, y, 0);
                    if (polygon.ContainsPoint(p)) points.Add(p);
                }
            }
        }

        return points;
    }

    private List<Vector3D> GenerateCircularPattern(Polygon2D polygon)
    {
        var points = new List<Vector3D>();
        var center = polygon.Centroid;
        var maxRadius = Math.Max(polygon.Bounds.Size.X, polygon.Bounds.Size.Y) / 2;

        for (double r = LineSpacing; r <= maxRadius; r += LineSpacing)
        {
            var circumference = 2 * Math.PI * r;
            var numPoints = (int)(circumference / (LineSpacing / 2));

            for (int i = 0; i <= numPoints; i++)
            {
                var angle = 2 * Math.PI * i / numPoints;
                var x = center.X + r * Math.Cos(angle);
                var y = center.Y + r * Math.Sin(angle);
                var point = new Vector3D(x, y, 0);

                if (polygon.ContainsPoint(point))
                    points.Add(point);
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

#endregion

#region Patrol Mission

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

#endregion

#region Delivery Mission

/// <summary>
/// Package delivery mission.
/// </summary>
public class DeliveryMission : DroneMission
{
    public override MissionType Type => MissionType.Delivery;

    /// <summary>Pickup location.</summary>
    public Vector3D PickupLocation { get; set; }

    /// <summary>Delivery location.</summary>
    public Vector3D DeliveryLocation { get; set; }

    /// <summary>Package weight in kg.</summary>
    public double PackageWeightKg { get; set; } = 1.0;

    /// <summary>Hover time at pickup (seconds).</summary>
    public double PickupHoverSec { get; set; } = 30.0;

    /// <summary>Hover time at delivery (seconds).</summary>
    public double DeliveryHoverSec { get; set; } = 30.0;

    /// <summary>Delivery altitude (lower for drop).</summary>
    public double DeliveryAltitude { get; set; } = 15.0;

    /// <summary>Return to home after delivery.</summary>
    public bool ReturnAfterDelivery { get; set; } = true;

    public double HoverTimeAtPickup { get; set; } = 10;    // seconds
    public double HoverTimeAtDelivery { get; set; } = 10;  // seconds

    public override FlightPath GenerateFlightPath()
    {
        var waypoints = new List<Waypoint>();
        var time = 0.0;

        // Start at home
        waypoints.Add(new Waypoint(HomePosition, time));
        time += 5;

        // Fly to cruise altitude
        var cruiseStart = HomePosition + new Vector3D(0, 0, Altitude);
        time += Altitude / 5; // Climb rate
        waypoints.Add(new Waypoint(cruiseStart, time));

        // Fly to pickup
        var pickupCruise = new Vector3D(PickupLocation.X, PickupLocation.Y, HomePosition.Z + Altitude);
        var toPickup = Vector3D.Distance(cruiseStart, pickupCruise);
        time += toPickup / Speed;
        waypoints.Add(new Waypoint(pickupCruise, time));

        // Descend for pickup
        var pickupPoint = new Vector3D(PickupLocation.X, PickupLocation.Y, PickupLocation.Z + 5);
        time += (Altitude - 5) / 3; // Descent rate
        waypoints.Add(new Waypoint(pickupPoint, time));

        // Hover for pickup
        time += PickupHoverSec;
        waypoints.Add(new Waypoint(pickupPoint, time));

        // Climb back up
        time += (Altitude - 5) / 5;
        waypoints.Add(new Waypoint(pickupCruise, time));

        // Fly to delivery
        var deliveryCruise = new Vector3D(DeliveryLocation.X, DeliveryLocation.Y, HomePosition.Z + Altitude);
        var toDelivery = Vector3D.Distance(pickupCruise, deliveryCruise);
        time += toDelivery / Speed;
        waypoints.Add(new Waypoint(deliveryCruise, time));

        // Descend for delivery
        var deliveryPoint = new Vector3D(DeliveryLocation.X, DeliveryLocation.Y, DeliveryLocation.Z + DeliveryAltitude);
        time += (Altitude - DeliveryAltitude) / 3;
        waypoints.Add(new Waypoint(deliveryPoint, time));

        // Hover for delivery
        time += DeliveryHoverSec;
        waypoints.Add(new Waypoint(deliveryPoint, time));

        if (ReturnAfterDelivery)
        {
            // Climb and return
            time += (Altitude - DeliveryAltitude) / 5;
            waypoints.Add(new Waypoint(deliveryCruise, time));

            var toHome = Vector3D.Distance(deliveryCruise, cruiseStart);
            time += toHome / Speed;
            waypoints.Add(new Waypoint(cruiseStart, time));

            // Land
            time += Altitude / 3;
            waypoints.Add(new Waypoint(HomePosition, time));
        }

        EstimatedDurationSec = time;
        EstimatedDistanceM = CalculateTotalDistance(waypoints);

        return FlightPath.CreateSpline(waypoints);
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

#endregion

#region Search and Rescue Mission

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

#endregion

#region Orbit Mission

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

#endregion

#region Waypoint Mission

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

#endregion
