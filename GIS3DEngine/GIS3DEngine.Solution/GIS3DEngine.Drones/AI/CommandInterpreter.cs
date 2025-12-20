using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Missions;

namespace GIS3DEngine.Drones.AI;

/// <summary>
/// AI-powered natural language command interpreter.
/// </summary>
public class CommandInterpreter
{
    private readonly AnthropicClient _client;

    private const string SystemPrompt = """
        אתה מפרש פקודות לרחפן (Drone). 
        המשתמש נותן פקודות בעברית או באנגלית, ואתה מחזיר JSON עם הפקודה המתאימה.
        
        פקודות זמינות:
        - arm: חימוש הרחפן
        - disarm: ניטרול
        - takeoff: המראה (עם altitude בעברית)
        - land: נחיתה
        - goto: טיסה לנקודה (עם x, y, z)
        - rtl: חזרה הביתה (Return To Launch)
        - hover: ריחוף במקום
        - emergency: עצירת חירום
        - survey: משימת סריקה (עם width, height, altitude)
        - patrol: משימת סיור (עם points)
        - orbit: הקפה סביב נקודה (עם center_x, center_y, radius)
        - status: בקשת סטטוס
        - help: עזרה
        
        החזר JSON בפורמט:
        {
            "command": "שם_הפקודה",
            "params": { ... פרמטרים רלוונטיים ... },
            "confidence": 0.0-1.0,
            "response": "תגובה למשתמש בעברית"
        }
        
        אם לא הבנת את הפקודה, החזר command: "unknown".
        """;

    public CommandInterpreter(AnthropicClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Interpret a natural language command.
    /// </summary>
    public async Task<DroneCommand> InterpretAsync(string userInput)
    {
        var response = await _client.SendMessageAsync(userInput, SystemPrompt);
        return ParseCommand(response);
    }

    /// <summary>
    /// Interpret with drone context.
    /// </summary>
    public async Task<DroneCommand> InterpretWithContextAsync(
        string userInput,
        DroneState state,
        string? currentMission = null)
    {
        var contextPrompt = $"""
            {SystemPrompt}
            
            מצב נוכחי של הרחפן:
            - סטטוס: {state.Status}
            - מיקום: ({state.Position.X:F1}, {state.Position.Y:F1}, {state.Position.Z:F1})
            - גובה: {state.AltitudeAGL:F1}m
            - סוללה: {state.BatteryPercent:F1}%
            - מרחק מהבית: {state.DistanceFromHome:F1}m
            - משימה נוכחית: {currentMission ?? "אין"}
            
            התחשב במצב הנוכחי בתשובתך.
            """;

        var response = await _client.SendMessageAsync(userInput, contextPrompt);
        return ParseCommand(response);
    }

    private DroneCommand ParseCommand(string jsonResponse)
    {
        try
        {
            // חילוץ JSON מהתשובה
            var jsonStart = jsonResponse.IndexOf('{');
            var jsonEnd = jsonResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = jsonResponse[jsonStart..(jsonEnd + 1)];
                return System.Text.Json.JsonSerializer.Deserialize<DroneCommand>(json)
                    ?? DroneCommand.Unknown("Failed to parse");
            }
        }
        catch { }

        return DroneCommand.Unknown("Failed to parse response");
    }
}

/// <summary>
/// Parsed drone command from AI.
/// </summary>
public class DroneCommand
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, object> Params { get; set; } = new();
    public double Confidence { get; set; }
    public string Response { get; set; } = string.Empty;

    public static DroneCommand Unknown(string reason) => new()
    {
        Command = "unknown",
        Confidence = 0,
        Response = reason
    };

    // Helper methods
    public double GetDouble(string key, double defaultValue = 0) =>
        Params.TryGetValue(key, out var val) ? Convert.ToDouble(val) : defaultValue;

    public int GetInt(string key, int defaultValue = 0) =>
        Params.TryGetValue(key, out var val) ? Convert.ToInt32(val) : defaultValue;

    public string GetString(string key, string defaultValue = "") =>
        Params.TryGetValue(key, out var val) ? val.ToString() ?? defaultValue : defaultValue;
}