using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.Drones.Telemetry;

namespace GIS3DEngine.Drones.AI;

/// <summary>
/// AI-powered drone assistant for natural interaction.
/// </summary>
public class DroneAssistant
{
    private readonly AnthropicClient _client;
    private readonly CommandInterpreter _interpreter;
    private readonly List<Message> _conversationHistory = new();

    private Drone? _activeDrone;
    private DroneFleetManager? _fleet;
    private TelemetryRecorder? _telemetry;

    private const string AssistantSystemPrompt = """
        אתה עוזר AI לניהול רחפנים. שמך "דרון" (Drone Assistant).
        
        אתה יכול:
        1. לענות על שאלות לגבי הרחפן והמשימות
        2. להמליץ על פעולות
        3. להסביר מצבים ובעיות
        4. לתכנן משימות
        5. לבצע פקודות (אם המשתמש מבקש)
        
        התנהג בצורה ידידותית ומקצועית.
        ענה בעברית אלא אם המשתמש פונה באנגלית.
        
        אם יש בעיית בטיחות (סוללה נמוכה, אות חלש וכו') - התריע!
        """;

    public DroneAssistant(string apiKey)
    {
        _client = new AnthropicClient(apiKey);
        _interpreter = new CommandInterpreter(_client);
    }

    #region Setup

    public void SetActiveDrone(Drone drone) => _activeDrone = drone;
    public void SetFleet(DroneFleetManager fleet) => _fleet = fleet;
    public void SetTelemetry(TelemetryRecorder telemetry) => _telemetry = telemetry;

    #endregion

    #region Chat

    /// <summary>
    /// Simple chat - for testing.
    /// </summary>
    public async Task<string> SimpleChatAsync(string message)
    {
        var context = _activeDrone != null
            ? $"Drone {_activeDrone.Id} is {_activeDrone.State.Status} with {_activeDrone.State.BatteryPercent}% battery. "
            : "";

        var fullMessage = context + "User says: " + message;

        return await _client.SimpleTestAsync(fullMessage);
    }

    /// <summary>
    /// Chat with the assistant.
    /// </summary>
    public async Task<AssistantResponse> ChatAsync(string userMessage)
    {
        // הוסף הקשר על מצב הרחפן
        var context = BuildContext();
        var fullPrompt = $"{AssistantSystemPrompt}\n\n{context}";

        _conversationHistory.Add(new Message { Role = "user", Content = userMessage });

        var response = await _client.SendConversationAsync(_conversationHistory, fullPrompt);

        _conversationHistory.Add(new Message { Role = "assistant", Content = response });

        // בדוק אם יש פקודה לביצוע
        var command = await TryExtractCommand(userMessage);

        return new AssistantResponse
        {
            Text = response,
            Command = command,
            HasCommand = command?.Command != "unknown" && command?.Confidence > 0.7
        };
    }

    /// <summary>
    /// Stream chat response.
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatAsync(string userMessage)
    {
        var context = BuildContext();
        var fullPrompt = $"{AssistantSystemPrompt}\n\n{context}";

        _conversationHistory.Add(new Message { Role = "user", Content = userMessage });

        var fullResponse = "";
        await foreach (var chunk in _client.StreamMessageAsync(
            string.Join("\n", _conversationHistory.Select(m => $"{m.Role}: {m.Content}")),
            fullPrompt))
        {
            fullResponse += chunk;
            yield return chunk;
        }

        _conversationHistory.Add(new Message { Role = "assistant", Content = fullResponse });
    }

    /// <summary>
    /// Clear conversation history.
    /// </summary>
    public void ClearHistory() => _conversationHistory.Clear();

    #endregion

    #region Commands

    /// <summary>
    /// Process a command from user input.
    /// </summary>
    public async Task<CommandResult> ProcessCommandAsync(string userInput)
    {
        if (_activeDrone == null)
            return CommandResult.Failure("אין רחפן פעיל. השתמש ב-SetActiveDrone()");

        var command = await _interpreter.InterpretWithContextAsync(
            userInput,
            _activeDrone.State,
            _activeDrone.CurrentMissionId);

        if (command.Command == "unknown" || command.Confidence < 0.5)
        {
            return CommandResult.Failure($"לא הבנתי את הפקודה. {command.Response}");
        }

        return await ExecuteCommandAsync(command);
    }

    private async Task<CommandResult> ExecuteCommandAsync(DroneCommand command)
    {
        if (_activeDrone == null)
            return CommandResult.Failure("אין רחפן פעיל");

        var result = command.Command.ToLower() switch
        {
            "arm" => ExecuteArm(),
            "disarm" => ExecuteDisarm(),
            "takeoff" => ExecuteTakeoff(command),
            "land" => ExecuteLand(),
            "goto" => ExecuteGoTo(command),
            "rtl" => ExecuteRTL(),
            "hover" => ExecuteHover(),
            "emergency" => ExecuteEmergency(),
            "survey" => await ExecuteSurvey(command),
            "patrol" => await ExecutePatrol(command),
            "orbit" => await ExecuteOrbit(command),
            "status" => ExecuteStatus(),
            _ => CommandResult.Failure($"פקודה לא מוכרת: {command.Command}")
        };

        result.AiResponse = command.Response;
        return result;
    }

    #endregion

    #region Command Execution

    private CommandResult ExecuteArm()
    {
        var success = _activeDrone!.Arm();
        return success
            ? CommandResult.Success("הרחפן מחומש ומוכן להמראה 🚁")
            : CommandResult.Failure("לא ניתן לחמש - בדוק סטטוס וסוללה");
    }

    private CommandResult ExecuteDisarm()
    {
        var success = _activeDrone!.Disarm();
        return success
            ? CommandResult.Success("הרחפן נוטרל")
            : CommandResult.Failure("לא ניתן לנטרל בזמן טיסה");
    }

    private CommandResult ExecuteTakeoff(DroneCommand command)
    {
        var altitude = command.GetDouble("altitude", 30);

        if (!_activeDrone!.State.IsArmed)
            _activeDrone.Arm();

        var success = _activeDrone.Takeoff(altitude);
        return success
            ? CommandResult.Success($"ממריא לגובה {altitude}m 🛫")
            : CommandResult.Failure("לא ניתן להמריא - בדוק סטטוס");
    }

    private CommandResult ExecuteLand()
    {
        var success = _activeDrone!.Land();
        return success
            ? CommandResult.Success("נוחת... 🛬")
            : CommandResult.Failure("לא ניתן לנחות במצב הנוכחי");
    }

    private CommandResult ExecuteGoTo(DroneCommand command)
    {
        var x = command.GetDouble("x");
        var y = command.GetDouble("y");
        var z = command.GetDouble("z", _activeDrone!.State.Position.Z);
        var speed = command.GetDouble("speed", 0);

        var target = new Vector3D(x, y, z);
        var success = _activeDrone.GoTo(target, speed);

        return success
            ? CommandResult.Success($"טס לנקודה ({x}, {y}, {z}) ✈️")
            : CommandResult.Failure("לא ניתן לטוס - הרחפן לא באוויר");
    }

    private CommandResult ExecuteRTL()
    {
        var success = _activeDrone!.ReturnToLaunch();
        return success
            ? CommandResult.Success("חוזר לנקודת ההמראה 🏠")
            : CommandResult.Failure("לא ניתן לחזור - הרחפן לא באוויר");
    }

    private CommandResult ExecuteHover()
    {
        var success = _activeDrone!.PauseMission();
        return success
            ? CommandResult.Success("מרחף במקום ⏸️")
            : CommandResult.Failure("לא ניתן לרחף במצב הנוכחי");
    }

    private CommandResult ExecuteEmergency()
    {
        _activeDrone!.EmergencyStop();
        return CommandResult.Success("🚨 עצירת חירום! נוחת מיידית!");
    }

    private async Task<CommandResult> ExecuteSurvey(DroneCommand command)
    {
        var width = command.GetDouble("width", 100);
        var height = command.GetDouble("height", 100);
        var altitude = command.GetDouble("altitude", 50);

        var mission = new SurveyMission
        {
            Name = "AI Survey Mission",
            AreaVertices = new List<Vector3D>
            {
                new(0, 0, 0),
                new(width, 0, 0),
                new(width, height, 0),
                new(0, height, 0)
            },
            Altitude = altitude,
            Speed = 10,
            HomePosition = _activeDrone!.HomePosition
        };

        var path = mission.GenerateFlightPath();

        if (!_activeDrone.State.IsArmed)
            _activeDrone.Arm();

        var success = _activeDrone.StartMission(mission.Id, path);

        return success
            ? CommandResult.Success($"מתחיל משימת סריקה: {width}x{height}m בגובה {altitude}m 📷")
            : CommandResult.Failure("לא ניתן להתחיל משימה");
    }

    private async Task<CommandResult> ExecutePatrol(DroneCommand command)
    {
        // TODO: Parse patrol points from command
        return CommandResult.Failure("משימת סיור דורשת הגדרת נקודות");
    }

    private async Task<CommandResult> ExecuteOrbit(DroneCommand command)
    {
        var centerX = command.GetDouble("center_x", _activeDrone!.State.Position.X);
        var centerY = command.GetDouble("center_y", _activeDrone.State.Position.Y);
        var radius = command.GetDouble("radius", 50);
        var altitude = command.GetDouble("altitude", _activeDrone.State.AltitudeAGL);

        var mission = new OrbitMission
        {
            Name = "AI Orbit Mission",
            OrbitCenter = new Vector3D(centerX, centerY, 0),
            OrbitRadius = radius,
            Altitude = altitude,
            Orbits = 1,
            HomePosition = _activeDrone.HomePosition
        };

        var path = mission.GenerateFlightPath();
        var success = _activeDrone.StartMission(mission.Id, path);

        return success
            ? CommandResult.Success($"מקיף נקודה ({centerX}, {centerY}) ברדיוס {radius}m 🔄")
            : CommandResult.Failure("לא ניתן להתחיל הקפה");
    }

    private CommandResult ExecuteStatus()
    {
        var state = _activeDrone!.State;
        var status = $"""
            📊 סטטוס הרחפן:
            ├─ מצב: {state.Status}
            ├─ מיקום: ({state.Position.X:F1}, {state.Position.Y:F1}, {state.Position.Z:F1})
            ├─ גובה: {state.AltitudeAGL:F1}m
            ├─ מהירות: {state.GroundSpeed:F1} m/s
            ├─ 🔋 סוללה: {state.BatteryPercent:F1}%
            ├─ 🏠 מרחק מהבית: {state.DistanceFromHome:F1}m
            └─ ⏱️ זמן טיסה: {state.FlightTimeSec:F0}s
            """;

        return CommandResult.Success(status);
    }

    #endregion

    #region Context Building

    private string BuildContext()
    {
        if (_activeDrone == null)
            return "אין רחפן פעיל.";

        var state = _activeDrone.State;
        var warnings = new List<string>();

        if (state.BatteryPercent < 20)
            warnings.Add("⚠️ סוללה נמוכה!");
        if (state.SignalStrength < 30)
            warnings.Add("⚠️ אות חלש!");
        if (state.IsFailsafe)
            warnings.Add("🚨 מצב חירום פעיל!");

        return $"""
            === מצב הרחפן ===
            ID: {_activeDrone.Id}
            סטטוס: {state.Status}
            מיקום: ({state.Position.X:F1}, {state.Position.Y:F1}, {state.Position.Z:F1})
            גובה: {state.AltitudeAGL:F1}m
            מהירות: {state.GroundSpeed:F1} m/s
            סוללה: {state.BatteryPercent:F1}%
            מרחק מהבית: {state.DistanceFromHome:F1}m
            משימה: {_activeDrone.CurrentMissionId ?? "אין"}
            {(warnings.Any() ? "\n" + string.Join("\n", warnings) : "")}
            """;
    }

    private async Task<DroneCommand?> TryExtractCommand(string message)
    {
        // מילות מפתח לפקודות
        var commandKeywords = new[] { "טוס", "המרא", "נחת", "עצור", "חמש", "סרוק", "fly", "takeoff", "land", "stop" };

        if (commandKeywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return await _interpreter.InterpretAsync(message);
        }

        return null;
    }

    #endregion
}

#region Response Models

public class AssistantResponse
{
    public string Text { get; set; } = string.Empty;
    public DroneCommand? Command { get; set; }
    public bool HasCommand { get; set; }
}

public class CommandResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AiResponse { get; set; }

    public static CommandResult Success(string message) => new() { IsSuccess = true, Message = message };
    public static CommandResult Failure(string message) => new() { IsSuccess = false, Message = message };
}

#endregion