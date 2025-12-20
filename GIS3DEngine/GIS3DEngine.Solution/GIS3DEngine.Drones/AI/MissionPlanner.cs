using System.Text.Json;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.Drones.Telemetry;

namespace GIS3DEngine.Drones.AI;

/// <summary>
/// AI-powered mission planner that creates optimal missions from natural language descriptions.
/// </summary>
public class MissionPlanner
{
    private readonly AnthropicClient _client;

    private const string PlannerSystemPrompt = """
        אתה מתכנן משימות לרחפנים. 
        המשתמש מתאר מה הוא רוצה לעשות, ואתה מחזיר תוכנית משימה בפורמט JSON.
        
        סוגי משימות זמינים:
        1. survey - סריקה/מיפוי של שטח
        2. patrol - סיור על מסלול
        3. delivery - משלוח חבילה
        4. search - חיפוש והצלה
        5. orbit - הקפה סביב נקודה
        6. waypoint - מסלול נקודות ציון
        
        החזר JSON בפורמט:
        {
            "mission_type": "סוג המשימה",
            "name": "שם המשימה",
            "description": "תיאור",
            "parameters": {
                // פרמטרים לפי סוג המשימה
            },
            "safety_notes": ["הערות בטיחות"],
            "estimated_duration_min": 0,
            "estimated_distance_m": 0,
            "recommended_altitude": 50,
            "recommended_speed": 10,
            "confidence": 0.0-1.0
        }
        
        פרמטרים לפי סוג:
        - survey: area_vertices (רשימת נקודות), pattern (lawnmower/spiral/grid), line_spacing
        - patrol: patrol_points (רשימת נקודות), loops, pause_at_waypoints
        - delivery: pickup (x,y,z), delivery (x,y,z), package_weight_kg
        - search: center (x,y), radius, grid_size, expanding_square (true/false)
        - orbit: center (x,y), radius, orbits, clockwise
        - waypoint: waypoints (רשימת x,y,z), speeds, pauses
        
        התחשב ב:
        - בטיחות (גובה מינימלי, מרחק מבניינים)
        - יעילות (מסלול קצר, חסכון בסוללה)
        - תנאי מזג אוויר אם צוינו
        - מגבלות הרחפן אם צוינו
        """;

    public MissionPlanner(AnthropicClient client)
    {
        _client = client;
    }

    public MissionPlanner(string apiKey)
    {
        _client = new AnthropicClient(apiKey);
    }

    #region Mission Planning

    /// <summary>
    /// Plan a mission from natural language description.
    /// </summary>
    public async Task<MissionPlan> PlanMissionAsync(string description)
    {
        var response = await _client.SendMessageAsync(description, PlannerSystemPrompt);
        return ParseMissionPlan(response);
    }

    /// <summary>
    /// Plan a mission with drone context.
    /// </summary>
    public async Task<MissionPlan> PlanMissionAsync(
        string description,
        DroneSpecifications specs,
        Vector3D homePosition,
        double currentBattery = 100)
    {
        var contextPrompt = $"""
            {PlannerSystemPrompt}
            
            מפרט הרחפן:
            - סוג: {specs.Type}
            - דגם: {specs.Model}
            - מהירות מקסימלית: {specs.MaxSpeedMs} m/s
            - גובה מקסימלי: {specs.MaxAltitudeM}m
            - זמן טיסה מקסימלי: {specs.MaxFlightTimeMinutes} דקות
            - סוללה נוכחית: {currentBattery}%
            
            מיקום הבית: ({homePosition.X}, {homePosition.Y}, {homePosition.Z})
            
            תכנן משימה שמתאימה למגבלות הרחפן.
            אם הסוללה נמוכה, תכנן משימה קצרה יותר.
            """;

        var response = await _client.SendMessageAsync(description, contextPrompt);
        return ParseMissionPlan(response);
    }

    /// <summary>
    /// Plan a mission with area constraints.
    /// </summary>
    public async Task<MissionPlan> PlanMissionInAreaAsync(
        string description,
        Polygon2D allowedArea,
        List<Polygon2D>? noFlyZones = null)
    {
        var areaDescription = $"שטח מותר: {string.Join(", ", allowedArea.Vertices.Select(v => $"({v.X},{v.Y})"))}";

        if (noFlyZones?.Any() == true)
        {
            areaDescription += "\nאזורים אסורים לטיסה:";
            for (int i = 0; i < noFlyZones.Count; i++)
            {
                areaDescription += $"\n- אזור {i + 1}: {string.Join(", ", noFlyZones[i].Vertices.Select(v => $"({v.X},{v.Y})"))}";
            }
        }

        var contextPrompt = $"""
            {PlannerSystemPrompt}
            
            מגבלות שטח:
            {areaDescription}
            
            וודא שכל נקודות המסלול נמצאות בתוך השטח המותר ומחוץ לאזורים האסורים.
            """;

        var response = await _client.SendMessageAsync(description, contextPrompt);
        return ParseMissionPlan(response);
    }

    #endregion

    #region Mission Generation

    /// <summary>
    /// Generate a DroneMission object from a plan.
    /// </summary>
    public DroneMission? GenerateMission(MissionPlan plan, Vector3D homePosition)
    {
        if (!plan.IsValid)
            return null;

        return plan.MissionType.ToLower() switch
        {
            "survey" => GenerateSurveyMission(plan, homePosition),
            "patrol" => GeneratePatrolMission(plan, homePosition),
            "delivery" => GenerateDeliveryMission(plan, homePosition),
            "search" => GenerateSearchMission(plan, homePosition),
            "orbit" => GenerateOrbitMission(plan, homePosition),
            "waypoint" => GenerateWaypointMission(plan, homePosition),
            _ => null
        };
    }

    private SurveyMission GenerateSurveyMission(MissionPlan plan, Vector3D homePosition)
    {
        var vertices = ParseVertices(plan.Parameters.GetValueOrDefault("area_vertices"));
        var pattern = ParseSurveyPattern(plan.Parameters.GetValueOrDefault("pattern")?.ToString());
        var lineSpacing = GetDouble(plan.Parameters, "line_spacing", 20);

        return new SurveyMission
        {
            Name = plan.Name,
            Description = plan.Description,
            AreaVertices = vertices.Any() ? vertices : DefaultSurveyArea(homePosition),
            Pattern = pattern,
            LineSpacing = lineSpacing,
            Altitude = plan.RecommendedAltitude,
            Speed = plan.RecommendedSpeed,
            HomePosition = homePosition
        };
    }

    private PatrolMission GeneratePatrolMission(MissionPlan plan, Vector3D homePosition)
    {
        var points = ParseVertices(plan.Parameters.GetValueOrDefault("patrol_points"));
        var loops = GetInt(plan.Parameters, "loops", 1);
        var pause = GetDouble(plan.Parameters, "pause_at_waypoints", 5);

        return new PatrolMission
        {
            Name = plan.Name,
            Description = plan.Description,
            PatrolPath = points.Any() ? points : DefaultPatrolPath(homePosition),
            Loops = loops,
            WaypointPauseSec = pause,
            Altitude = plan.RecommendedAltitude,
            Speed = plan.RecommendedSpeed,
            HomePosition = homePosition
        };
    }

    private DeliveryMission GenerateDeliveryMission(MissionPlan plan, Vector3D homePosition)
    {
        var pickup = ParseVector3D(plan.Parameters.GetValueOrDefault("pickup")) ?? homePosition;
        var delivery = ParseVector3D(plan.Parameters.GetValueOrDefault("delivery")) ?? homePosition + new Vector3D(100, 100, 0);
        var weight = GetDouble(plan.Parameters, "package_weight_kg", 1);

        return new DeliveryMission
        {
            Name = plan.Name,
            Description = plan.Description,
            PickupLocation = pickup,
            DeliveryLocation = delivery,
            PackageWeightKg = weight,
            Altitude = plan.RecommendedAltitude,
            Speed = plan.RecommendedSpeed,
            HomePosition = homePosition
        };
    }

    private SearchAndRescueMission GenerateSearchMission(MissionPlan plan, Vector3D homePosition)
    {
        var center = ParseVector3D(plan.Parameters.GetValueOrDefault("center")) ?? homePosition;
        var radius = GetDouble(plan.Parameters, "radius", 200);
        var gridSize = GetDouble(plan.Parameters, "grid_size", 30);
        var expanding = GetBool(plan.Parameters, "expanding_square", true);

        return new SearchAndRescueMission
        {
            Name = plan.Name,
            Description = plan.Description,
            SearchCenter = center,
            SearchRadius = radius,
            GridSize = gridSize,
            ExpandingSquare = expanding,
            Altitude = plan.RecommendedAltitude,
            Speed = plan.RecommendedSpeed,
            HomePosition = homePosition
        };
    }

    private OrbitMission GenerateOrbitMission(MissionPlan plan, Vector3D homePosition)
    {
        var center = ParseVector3D(plan.Parameters.GetValueOrDefault("center")) ?? homePosition + new Vector3D(50, 50, 0);
        var radius = GetDouble(plan.Parameters, "radius", 50);
        var orbits = GetInt(plan.Parameters, "orbits", 2);
        var clockwise = GetBool(plan.Parameters, "clockwise", true);

        return new OrbitMission
        {
            Name = plan.Name,
            Description = plan.Description,
            OrbitCenter = center,
            OrbitRadius = radius,
            Orbits = orbits,
            Clockwise = clockwise,
            Altitude = plan.RecommendedAltitude,
            Speed = plan.RecommendedSpeed,
            HomePosition = homePosition
        };
    }

    private WaypointMission GenerateWaypointMission(MissionPlan plan, Vector3D homePosition)
    {
        var waypoints = ParseVertices(plan.Parameters.GetValueOrDefault("waypoints"));

        return new WaypointMission
        {
            Name = plan.Name,
            Description = plan.Description,
            Waypoints = waypoints.Any() ? waypoints : DefaultWaypoints(homePosition),
            Altitude = plan.RecommendedAltitude,
            Speed = plan.RecommendedSpeed,
            HomePosition = homePosition
        };
    }

    #endregion

    #region Optimization

    /// <summary>
    /// Optimize an existing mission using AI.
    /// </summary>
    public async Task<MissionPlan> OptimizeMissionAsync(
        DroneMission mission,
        string optimizationGoal = "מינימום זמן וצריכת סוללה")
    {
        var missionDescription = $"""
            משימה קיימת:
            - סוג: {mission.Type}
            - שם: {mission.Name}
            - גובה: {mission.Altitude}m
            - מהירות: {mission.Speed} m/s
            - משך משוער: {mission.EstimatedDurationSec / 60:F1} דקות
            - מרחק משוער: {mission.EstimatedDistanceM:F0}m
            
            מטרת האופטימיזציה: {optimizationGoal}
            
            הצע שיפורים למשימה והחזר תוכנית משופרת.
            """;

        return await PlanMissionAsync(missionDescription);
    }

    /// <summary>
    /// Suggest mission based on telemetry analysis.
    /// </summary>
    public async Task<MissionPlan> SuggestMissionFromTelemetryAsync(
        TelemetryRecorder recorder,
        string droneId,
        string goal)
    {
        var history = recorder.GetHistory(droneId, 100);
        var avgSpeed = history.Any() ? history.Average(p => p.GroundSpeed) : 10;
        var avgAltitude = history.Any() ? history.Average(p => p.AltitudeAGL) : 50;
        var batteryUsage = history.Count > 1
            ? history.First().BatteryPercent - history.Last().BatteryPercent
            : 0;

        var telemetryContext = $"""
            נתוני טלמטריה אחרונים:
            - מהירות ממוצעת: {avgSpeed:F1} m/s
            - גובה ממוצע: {avgAltitude:F1}m
            - צריכת סוללה: {batteryUsage:F1}% ב-{history.Count} דגימות
            
            מטרה: {goal}
            
            תכנן משימה שמתאימה לביצועים בפועל של הרחפן.
            """;

        return await PlanMissionAsync(telemetryContext);
    }

    #endregion

    #region Parsing Helpers

    private MissionPlan ParseMissionPlan(string jsonResponse)
    {
        try
        {
            var jsonStart = jsonResponse.IndexOf('{');
            var jsonEnd = jsonResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = jsonResponse[jsonStart..(jsonEnd + 1)];
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<MissionPlan>(json, options) ?? MissionPlan.Invalid("Parse failed");
            }
        }
        catch (Exception ex)
        {
            return MissionPlan.Invalid($"Parse error: {ex.Message}");
        }

        return MissionPlan.Invalid("No JSON found in response");
    }

    private List<Vector3D> ParseVertices(object? value)
    {
        if (value == null) return new List<Vector3D>();

        try
        {
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                var result = new List<Vector3D>();
                foreach (var item in element.EnumerateArray())
                {
                    var vec = ParseVector3D(item);
                    if (vec.HasValue)
                        result.Add(vec.Value);
                }
                return result;
            }
        }
        catch { }

        return new List<Vector3D>();
    }

    private Vector3D? ParseVector3D(object? value)
    {
        if (value == null) return null;

        try
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var x = element.TryGetProperty("x", out var xProp) ? xProp.GetDouble() : 0;
                    var y = element.TryGetProperty("y", out var yProp) ? yProp.GetDouble() : 0;
                    var z = element.TryGetProperty("z", out var zProp) ? zProp.GetDouble() : 0;
                    return new Vector3D(x, y, z);
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    var arr = element.EnumerateArray().ToList();
                    if (arr.Count >= 2)
                    {
                        return new Vector3D(
                            arr[0].GetDouble(),
                            arr[1].GetDouble(),
                            arr.Count > 2 ? arr[2].GetDouble() : 0);
                    }
                }
            }
        }
        catch { }

        return null;
    }

    private SurveyPattern ParseSurveyPattern(string? pattern) => pattern?.ToLower() switch
    {
        "spiral" or "spiraloutward" => SurveyPattern.SpiralOutward,
        "spiralinward" => SurveyPattern.SpiralInward,
        "grid" => SurveyPattern.Grid,
        "circular" => SurveyPattern.Circular,
        _ => SurveyPattern.Lawnmower
    };

    private double GetDouble(Dictionary<string, object> dict, string key, double defaultValue)
    {
        if (dict.TryGetValue(key, out var val))
        {
            if (val is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                return elem.GetDouble();
            if (val is double d) return d;
            if (double.TryParse(val?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    private int GetInt(Dictionary<string, object> dict, string key, int defaultValue)
    {
        if (dict.TryGetValue(key, out var val))
        {
            if (val is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                return elem.GetInt32();
            if (val is int i) return i;
            if (int.TryParse(val?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    private bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue)
    {
        if (dict.TryGetValue(key, out var val))
        {
            if (val is JsonElement elem && elem.ValueKind == JsonValueKind.True) return true;
            if (val is JsonElement elem2 && elem2.ValueKind == JsonValueKind.False) return false;
            if (val is bool b) return b;
            if (bool.TryParse(val?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    #endregion

    #region Default Generators

    private List<Vector3D> DefaultSurveyArea(Vector3D home) => new()
    {
        home + new Vector3D(0, 0, 0),
        home + new Vector3D(100, 0, 0),
        home + new Vector3D(100, 100, 0),
        home + new Vector3D(0, 100, 0)
    };

    private List<Vector3D> DefaultPatrolPath(Vector3D home) => new()
    {
        home + new Vector3D(50, 0, 0),
        home + new Vector3D(50, 50, 0),
        home + new Vector3D(0, 50, 0),
        home + new Vector3D(0, 0, 0)
    };

    private List<Vector3D> DefaultWaypoints(Vector3D home) => new()
    {
        home + new Vector3D(30, 0, 50),
        home + new Vector3D(60, 30, 50),
        home + new Vector3D(30, 60, 50),
        home + new Vector3D(0, 30, 50)
    };

    #endregion
}

#region Mission Plan Model

/// <summary>
/// AI-generated mission plan.
/// </summary>
public class MissionPlan
{
    [System.Text.Json.Serialization.JsonPropertyName("mission_type")]
    public string MissionType { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("safety_notes")]
    public List<string> SafetyNotes { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("estimated_duration_min")]
    public double EstimatedDurationMin { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("estimated_distance_m")]
    public double EstimatedDistanceM { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("recommended_altitude")]
    public double RecommendedAltitude { get; set; } = 50;

    [System.Text.Json.Serialization.JsonPropertyName("recommended_speed")]
    public double RecommendedSpeed { get; set; } = 10;

    [System.Text.Json.Serialization.JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    public bool IsValid => !string.IsNullOrEmpty(MissionType) && Confidence > 0.5;

    public string? ErrorMessage { get; set; }

    public static MissionPlan Invalid(string error) => new()
    {
        Confidence = 0,
        ErrorMessage = error
    };

    public override string ToString() => $"""
        📋 תוכנית משימה: {Name}
        ├─ סוג: {MissionType}
        ├─ תיאור: {Description}
        ├─ גובה מומלץ: {RecommendedAltitude}m
        ├─ מהירות מומלצת: {RecommendedSpeed} m/s
        ├─ משך משוער: {EstimatedDurationMin:F1} דקות
        ├─ מרחק משוער: {EstimatedDistanceM:F0}m
        ├─ ביטחון: {Confidence:P0}
        └─ הערות בטיחות: {string.Join(", ", SafetyNotes)}
        """;
}

#endregion