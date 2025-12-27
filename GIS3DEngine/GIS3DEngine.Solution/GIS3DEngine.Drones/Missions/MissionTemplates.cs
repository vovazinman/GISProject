using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using Microsoft.Extensions.Logging;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Interface for mission generation.
/// </summary>
public interface IMissionFactory
{
    FlightPath CreateOrbit(Vector3D center, double radius, double altitude, double speed, int laps = 1);
    FlightPath CreateSurvey(Vector3D start, double width, double height, double altitude, double speed);
    FlightPath CreatePatrol(IEnumerable<Vector3D> points, double altitude, double speed, bool loop = true);
    FlightPath CreateFigureEight(Vector3D center, double size, double altitude, double speed);
    FlightPath CreateSpiral(Vector3D center, double radius, double startAlt, double endAlt, double speed);
    FlightPath CreateSearchPattern(Vector3D center, double maxSize, double altitude, double speed);
    FlightPath CreateDemoFlight(Vector3D home, double speed);
    FlightPath CreateFromTemplate(string templateId, Vector3D position, Dictionary<string, object> parameters);
    List<MissionTemplateInfo> GetTemplates();
}

/// <summary>
/// Mission factory - creates flight paths from templates.
/// </summary>
public class MissionFactory : IMissionFactory
{
    private readonly ILogger<MissionFactory>? _logger;

    public MissionFactory(ILogger<MissionFactory>? logger = null)
    {
        _logger = logger;
    }

    // ========== Mission Creation Methods ==========

    public FlightPath CreateOrbit(Vector3D center, double radius, double altitude, double speed, int laps = 1)
    {
        _logger?.LogInformation("Creating orbit: center=({X},{Y}), radius={R}m, alt={A}m",
            center.X, center.Y, radius, altitude);

        var circumference = 2 * Math.PI * radius;
        var duration = (circumference / speed) * laps;
        var segments = 36 * laps;

        var waypoints = new List<Waypoint>();
        var angleStep = 2 * Math.PI * laps / segments;
        var timeStep = duration / segments;

        for (int i = 0; i <= segments; i++)
        {
            var angle = i * angleStep;
            var position = new Vector3D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle),
                altitude
            );
            waypoints.Add(new Waypoint(position, i * timeStep, speed));
        }

        return FlightPath.CreateSpline(waypoints, isLooping: laps > 1);
    }

    public FlightPath CreateSurvey(Vector3D start, double width, double height, double altitude, double speed)
    {
        _logger?.LogInformation("Creating survey: {W}x{H}m at {A}m altitude", width, height, altitude);

        var waypoints = new List<Waypoint>();
        var time = 0.0;
        var currentPos = new Vector3D(start.X, start.Y, altitude);
        var lineSpacing = 20.0; // meters between lines

        var numLines = (int)Math.Ceiling(height / lineSpacing);

        waypoints.Add(new Waypoint(currentPos, time, speed));

        for (int i = 0; i < numLines; i++)
        {
            Vector3D nextPos;
            if (i % 2 == 0)
            {
                nextPos = new Vector3D(start.X + width, start.Y + i * lineSpacing, altitude);
            }
            else
            {
                nextPos = new Vector3D(start.X, start.Y + i * lineSpacing, altitude);
            }

            var distance = Vector3D.Distance(currentPos, nextPos);
            time += distance / speed;
            waypoints.Add(new Waypoint(nextPos, time, speed));
            currentPos = nextPos;

            if (i < numLines - 1)
            {
                nextPos = new Vector3D(currentPos.X, start.Y + (i + 1) * lineSpacing, altitude);
                distance = Vector3D.Distance(currentPos, nextPos);
                time += distance / speed;
                waypoints.Add(new Waypoint(nextPos, time, speed));
                currentPos = nextPos;
            }
        }

        return FlightPath.CreateLinear(waypoints);
    }

    public FlightPath CreatePatrol(IEnumerable<Vector3D> points, double altitude, double speed, bool loop = true)
    {
        _logger?.LogInformation("Creating patrol: {Count} points, loop={Loop}", points.Count(), loop);

        var waypoints = new List<Waypoint>();
        var time = 0.0;
        Vector3D? prevPos = null;

        foreach (var point in points)
        {
            var pos = new Vector3D(point.X, point.Y, altitude);

            if (prevPos.HasValue)
            {
                var distance = Vector3D.Distance(prevPos.Value, pos);
                time += distance / speed;
            }

            waypoints.Add(new Waypoint(pos, time, speed));
            prevPos = pos;
        }

        if (loop && waypoints.Count > 1)
        {
            var distance = Vector3D.Distance(waypoints[^1].Position, waypoints[0].Position);
            time += distance / speed;
            waypoints.Add(new Waypoint(waypoints[0].Position, time, speed));
        }

        return FlightPath.CreateLinear(waypoints, loop);
    }

    public FlightPath CreateFigureEight(Vector3D center, double size, double altitude, double speed)
    {
        _logger?.LogInformation("Creating figure-8: size={S}m at {A}m altitude", size, altitude);

        var segments = 72;
        var waypoints = new List<Waypoint>();
        var time = 0.0;
        Vector3D? prevPos = null;

        for (int i = 0; i <= segments; i++)
        {
            var t = (double)i / segments * 2 * Math.PI;
            var position = new Vector3D(
                center.X + size * Math.Sin(t),
                center.Y + size * Math.Sin(t) * Math.Cos(t),
                altitude
            );

            if (prevPos.HasValue)
            {
                var distance = Vector3D.Distance(prevPos.Value, position);
                time += distance / speed;
            }

            waypoints.Add(new Waypoint(position, time, speed));
            prevPos = position;
        }

        return FlightPath.CreateSpline(waypoints, isLooping: true);
    }

    public FlightPath CreateSpiral(Vector3D center, double radius, double startAlt, double endAlt, double speed)
    {
        _logger?.LogInformation("Creating spiral: {Start}m to {End}m altitude", startAlt, endAlt);

        var revolutions = 3;
        var segments = 36 * revolutions;
        var waypoints = new List<Waypoint>();
        var time = 0.0;
        var altitudeStep = (endAlt - startAlt) / segments;
        var angleStep = 2 * Math.PI / 36;

        Vector3D? prevPos = null;

        for (int i = 0; i <= segments; i++)
        {
            var angle = i * angleStep;
            var altitude = startAlt + i * altitudeStep;

            var pos = new Vector3D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle),
                altitude
            );

            if (prevPos.HasValue)
            {
                var distance = Vector3D.Distance(prevPos.Value, pos);
                time += distance / speed;
            }

            waypoints.Add(new Waypoint(pos, time, speed));
            prevPos = pos;
        }

        return FlightPath.CreateSpline(waypoints);
    }

    public FlightPath CreateSearchPattern(Vector3D center, double maxSize, double altitude, double speed)
    {
        _logger?.LogInformation("Creating search pattern: max {S}m at {A}m altitude", maxSize, altitude);

        var waypoints = new List<Waypoint>();
        var time = 0.0;
        var currentPos = new Vector3D(center.X, center.Y, altitude);
        var spacing = 30.0;

        waypoints.Add(new Waypoint(currentPos, time, speed));

        var directions = new[]
        {
            new Vector3D(1, 0, 0),
            new Vector3D(0, 1, 0),
            new Vector3D(-1, 0, 0),
            new Vector3D(0, -1, 0)
        };

        var size = spacing;
        var dirIndex = 0;

        while (size <= maxSize)
        {
            for (int leg = 0; leg < 2; leg++)
            {
                var dir = directions[dirIndex % 4];
                var nextPos = new Vector3D(
                    currentPos.X + dir.X * size,
                    currentPos.Y + dir.Y * size,
                    altitude
                );

                var distance = Vector3D.Distance(currentPos, nextPos);
                time += distance / speed;
                waypoints.Add(new Waypoint(nextPos, time, speed));
                currentPos = nextPos;
                dirIndex++;
            }

            size += spacing;
        }

        return FlightPath.CreateLinear(waypoints);
    }

    public FlightPath CreateDemoFlight(Vector3D home, double speed)
    {
        _logger?.LogInformation("Creating demo flight from home ({X},{Y})", home.X, home.Y);

        var waypoints = new List<Waypoint>();
        var time = 0.0;
        var currentPos = home;

        void AddWaypoint(double x, double y, double z)
        {
            var pos = new Vector3D(x, y, z);
            var distance = Vector3D.Distance(currentPos, pos);
            time += distance / speed;
            waypoints.Add(new Waypoint(pos, time, speed));
            currentPos = pos;
        }

        waypoints.Add(new Waypoint(new Vector3D(home.X, home.Y, 0), 0, speed));

        AddWaypoint(home.X, home.Y, 50);
        AddWaypoint(home.X, home.Y + 200, 50);
        AddWaypoint(home.X + 200, home.Y + 200, 80);

        for (int i = 0; i <= 8; i++)
        {
            var angle = i * Math.PI / 4;
            AddWaypoint(
                home.X + 200 + 50 * Math.Cos(angle),
                home.Y + 200 + 50 * Math.Sin(angle),
                80
            );
        }

        AddWaypoint(home.X - 100, home.Y - 100, 60);
        AddWaypoint(home.X, home.Y, 50);
        AddWaypoint(home.X, home.Y, 0);

        return FlightPath.CreateSpline(waypoints);
    }

    // ========== Template Methods ==========

    public FlightPath CreateFromTemplate(string templateId, Vector3D position, Dictionary<string, object> parameters)
    {
        double GetParam(string key, double defaultVal) =>
            parameters.TryGetValue(key, out var val) ? Convert.ToDouble(val) : defaultVal;

        return templateId.ToLower() switch
        {
            "orbit" => CreateOrbit(
                position,
                radius: GetParam("radius", 50),
                altitude: GetParam("altitude", 50),
                speed: GetParam("speed", 10),
                laps: (int)GetParam("laps", 1)
            ),

            "survey" => CreateSurvey(
                position,
                width: GetParam("width", 100),
                height: GetParam("height", 100),
                altitude: GetParam("altitude", 50),
                speed: GetParam("speed", 8)
            ),

            "figure8" => CreateFigureEight(
                position,
                size: GetParam("size", 100),
                altitude: GetParam("altitude", 50),
                speed: GetParam("speed", 10)
            ),

            "spiral" => CreateSpiral(
                position,
                radius: GetParam("radius", 50),
                startAlt: GetParam("startAlt", 30),
                endAlt: GetParam("endAlt", 100),
                speed: GetParam("speed", 8)
            ),

            "search" => CreateSearchPattern(
                position,
                maxSize: GetParam("size", 200),
                altitude: GetParam("altitude", 40),
                speed: GetParam("speed", 10)
            ),

            "demo" => CreateDemoFlight(
                position,
                speed: GetParam("speed", 15)
            ),

            "tlv_tour" => CreateTelAvivTour(
                altitude: GetParam("altitude", 150),
                speed: GetParam("speed", 20)
            ),

            "coastal" => CreateCoastalPatrol(
                altitude: GetParam("altitude", 100),
                speed: GetParam("speed", 25)
            ),

            _ => throw new ArgumentException($"Unknown template: {templateId}")
        };
    }

    public List<MissionTemplateInfo> GetTemplates() => new()
    {
        new MissionTemplateInfo
        {
            Id = "orbit",
            Name = "Orbit",
            NameHe = "הקפה",
            Description = "Circle around a point",
            Icon = "🔄",
            Category = "Basic",
            Parameters = new()
            {
                ["radius"] = new() { Name = "Radius", DefaultValue = 50, Min = 10, Max = 500, Unit = "m" },
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 50, Min = 20, Max = 300, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 10, Min = 5, Max = 30, Unit = "m/s" },
                ["laps"] = new() { Name = "Laps", DefaultValue = 1, Min = 1, Max = 10, Unit = "" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "survey",
            Name = "Survey Area",
            NameHe = "סריקת שטח",
            Description = "Scan a rectangular area",
            Icon = "📷",
            Category = "Mapping",
            Parameters = new()
            {
                ["width"] = new() { Name = "Width", DefaultValue = 100, Min = 20, Max = 1000, Unit = "m" },
                ["height"] = new() { Name = "Height", DefaultValue = 100, Min = 20, Max = 1000, Unit = "m" },
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 50, Min = 20, Max = 200, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 8, Min = 3, Max = 20, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "patrol",
            Name = "Patrol",
            NameHe = "סיור",
            Description = "Patrol between waypoints",
            Icon = "🛡️",
            Category = "Security",
            Parameters = new()
            {
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 50, Min = 20, Max = 200, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 12, Min = 5, Max = 30, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "figure8",
            Name = "Figure 8",
            NameHe = "שמונה",
            Description = "Figure-8 pattern",
            Icon = "♾️",
            Category = "Demo",
            Parameters = new()
            {
                ["size"] = new() { Name = "Size", DefaultValue = 100, Min = 30, Max = 300, Unit = "m" },
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 50, Min = 20, Max = 200, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 10, Min = 5, Max = 25, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "spiral",
            Name = "Spiral",
            NameHe = "ספירלה",
            Description = "Spiral ascent/descent",
            Icon = "🌀",
            Category = "Demo",
            Parameters = new()
            {
                ["radius"] = new() { Name = "Radius", DefaultValue = 50, Min = 20, Max = 200, Unit = "m" },
                ["startAlt"] = new() { Name = "Start Altitude", DefaultValue = 30, Min = 10, Max = 200, Unit = "m" },
                ["endAlt"] = new() { Name = "End Altitude", DefaultValue = 100, Min = 20, Max = 300, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 8, Min = 3, Max = 20, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "search",
            Name = "Search Pattern",
            NameHe = "תבנית חיפוש",
            Description = "Expanding square search",
            Icon = "🔍",
            Category = "SAR",
            Parameters = new()
            {
                ["size"] = new() { Name = "Max Size", DefaultValue = 200, Min = 50, Max = 500, Unit = "m" },
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 40, Min = 20, Max = 150, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 10, Min = 5, Max = 20, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "demo",
            Name = "Demo Flight",
            NameHe = "טיסת הדגמה",
            Description = "Demonstration with various maneuvers",
            Icon = "🎬",
            Category = "Demo",
            Parameters = new()
            {
                ["speed"] = new() { Name = "Speed", DefaultValue = 15, Min = 8, Max = 30, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "tlv_tour",
            Name = "Tel Aviv Tour",
            NameHe = "סיור תל אביב",
            Description = "Tour Tel Aviv landmarks",
            Icon = "🏙️",
            Category = "Tours",
            Parameters = new()
            {
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 150, Min = 50, Max = 300, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 20, Min = 10, Max = 40, Unit = "m/s" }
            }
        },
        new MissionTemplateInfo
        {
            Id = "coastal",
            Name = "Coastal Patrol",
            NameHe = "סיור חופי",
            Description = "Patrol along the coastline",
            Icon = "🌊",
            Category = "Tours",
            Parameters = new()
            {
                ["altitude"] = new() { Name = "Altitude", DefaultValue = 100, Min = 50, Max = 200, Unit = "m" },
                ["speed"] = new() { Name = "Speed", DefaultValue = 25, Min = 15, Max = 50, Unit = "m/s" }
            }
        }
    };

    // ========== Predefined Tours ==========

    private FlightPath CreateTelAvivTour(double altitude, double speed)
    {
        var landmarks = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(1000, 500, 0),
            new Vector3D(-1000, 4500, 0),
            new Vector3D(0, 4000, 0),
            new Vector3D(-500, 1500, 0),
            new Vector3D(500, 500, 0),
            new Vector3D(1500, 0, 0),
            new Vector3D(0, 0, 0),
        };

        return CreatePatrol(landmarks, altitude, speed, loop: false);
    }

    private FlightPath CreateCoastalPatrol(double altitude, double speed)
    {
        var coastline = new[]
        {
            new Vector3D(-2000, -4000, 0),
            new Vector3D(-1500, -2500, 0),
            new Vector3D(-1000, 0, 0),
            new Vector3D(-1000, 4500, 0),
            new Vector3D(-2000, 8000, 0),
        };

        return CreatePatrol(coastline, altitude, speed, loop: false);
    }
}

// ========== DTOs ==========

public class MissionTemplateInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameHe { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public string Category { get; set; } = "General";
    public Dictionary<string, ParameterInfo> Parameters { get; set; } = new();
}

public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "number";
    public double DefaultValue { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public string Unit { get; set; } = "m";
}