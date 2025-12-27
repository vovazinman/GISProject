using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Primitives;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Survey/mapping mission with various scan patterns.
/// </summary>
public class SurveyMission : DroneMission
{
    public override MissionType Type => MissionType.Survey;

    /// <summary>Survey area origin point (bottom-left corner).</summary>
    public Vector3D Origin { get; set; } = Vector3D.Zero;

    /// <summary>Survey area width in meters.</summary>
    public double Width { get; set; } = 100;

    /// <summary>Survey area height in meters.</summary>
    public double Height { get; set; } = 100;

    /// <summary>Spacing between scan lines in meters.</summary>
    public double LineSpacing { get; set; } = 20;

    /// <summary>Survey scan pattern.</summary>
    public SurveyPattern Pattern { get; set; } = SurveyPattern.Lawnmower;

    /// <summary>Camera overlap percentage (for photogrammetry).</summary>
    public double OverlapPercent { get; set; } = 70;

    /// <summary>Survey area vertices (alternative to Origin/Width/Height).</summary>
    public List<Vector3D>? AreaVertices { get; set; }


    public override FlightPath GenerateFlightPath()
    {
        var waypoints = Pattern switch
        {
            SurveyPattern.SpiralOutward => GenerateSpiralWaypoints(),
            SurveyPattern.Grid => GenerateGridWaypoints(),
            _ => GenerateLawnmowerWaypoints()
        };

        var path = FlightPath.CreateWithSpeed(waypoints, Speed);

        EstimatedDurationSec = path.TotalDuration;
        EstimatedDistanceM = path.TotalDistance;

        return path;
    }

    public override MissionValidationResult Validate()
    {
        var result = base.Validate();

        if (Width < 10)
            result.Errors.Add("Width must be at least 10 meters");

        if (Height < 10)
            result.Errors.Add("Height must be at least 10 meters");

        if (LineSpacing < 1)
            result.Errors.Add("Line spacing must be at least 1 meter");

        if (LineSpacing > Math.Min(Width, Height))
            result.Warnings.Add("Line spacing is larger than survey area");

        if (OverlapPercent < 0 || OverlapPercent > 95)
            result.Warnings.Add("Overlap should be between 0-95%");

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Get the survey bounds.
    /// </summary>
    public (Vector3D Min, Vector3D Max) GetBounds()
    {
        if (AreaVertices != null && AreaVertices.Count >= 3)
        {
            var minX = AreaVertices.Min(v => v.X);
            var maxX = AreaVertices.Max(v => v.X);
            var minY = AreaVertices.Min(v => v.Y);
            var maxY = AreaVertices.Max(v => v.Y);

            return (
                new Vector3D(minX, minY, Altitude),
                new Vector3D(maxX, maxY, Altitude)
            );
        }

        return (
            Origin,
            new Vector3D(Origin.X + Width, Origin.Y + Height, Altitude)
        );
    }

    private List<Vector3D> GenerateLawnmowerWaypoints()
    {
        var waypoints = new List<Vector3D>();
        var lines = (int)Math.Ceiling(Height / LineSpacing);

        for (int i = 0; i <= lines; i++)
        {
            var y = Origin.Y + Math.Min(i * LineSpacing, Height);

            if (i % 2 == 0)
            {
                // Left to right
                waypoints.Add(new Vector3D(Origin.X, y, Altitude));
                waypoints.Add(new Vector3D(Origin.X + Width, y, Altitude));
            }
            else
            {
                // Right to left
                waypoints.Add(new Vector3D(Origin.X + Width, y, Altitude));
                waypoints.Add(new Vector3D(Origin.X, y, Altitude));
            }
        }

        return waypoints;
    }

    private List<Vector3D> GenerateSpiralWaypoints()
    {
        var waypoints = new List<Vector3D>();
        var centerX = Origin.X + Width / 2;
        var centerY = Origin.Y + Height / 2;
        var maxRadius = Math.Min(Width, Height) / 2;
        var turns = maxRadius / LineSpacing;
        var segments = (int)(turns * 36);

        for (int i = 0; i <= segments; i++)
        {
            var angle = i * (2 * Math.PI * turns) / segments;
            var radius = (maxRadius * i) / segments;

            waypoints.Add(new Vector3D(
                centerX + radius * Math.Cos(angle),
                centerY + radius * Math.Sin(angle),
                Altitude));
        }

        return waypoints;
    }

    private List<Vector3D> GenerateGridWaypoints()
    {
        var waypoints = new List<Vector3D>();

        // Horizontal lines
        var hLines = (int)Math.Ceiling(Height / LineSpacing);
        for (int i = 0; i <= hLines; i++)
        {
            var y = Origin.Y + Math.Min(i * LineSpacing, Height);

            if (i % 2 == 0)
            {
                waypoints.Add(new Vector3D(Origin.X, y, Altitude));
                waypoints.Add(new Vector3D(Origin.X + Width, y, Altitude));
            }
            else
            {
                waypoints.Add(new Vector3D(Origin.X + Width, y, Altitude));
                waypoints.Add(new Vector3D(Origin.X, y, Altitude));
            }
        }

        // Vertical lines
        var vLines = (int)Math.Ceiling(Width / LineSpacing);
        for (int i = 0; i <= vLines; i++)
        {
            var x = Origin.X + Math.Min(i * LineSpacing, Width);

            if (i % 2 == 0)
            {
                waypoints.Add(new Vector3D(x, Origin.Y, Altitude));
                waypoints.Add(new Vector3D(x, Origin.Y + Height, Altitude));
            }
            else
            {
                waypoints.Add(new Vector3D(x, Origin.Y + Height, Altitude));
                waypoints.Add(new Vector3D(x, Origin.Y, Altitude));
            }
        }

        return waypoints;
    }
}