using System.Text.Json;
using System.Text.Json.Serialization;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.Drones.AI;

/// <summary>
/// AI-powered natural language command interpreter.
/// </summary>
public class CommandInterpreter
{
    private readonly AnthropicClient _client;

    private const string SystemPrompt = """
        You are an advanced drone command interpreter and flight controller AI.
        Your role is to parse natural language commands (Hebrew or English) and convert them to precise JSON instructions.
        
        ═══════════════════════════════════════════════════════════════════════════════
        AVAILABLE COMMANDS
        ═══════════════════════════════════════════════════════════════════════════════
        
        | Command | Description | Required Params | Optional Params |
        |---------|-------------|-----------------|-----------------|
        | arm | Arm the drone motors | none | none |
        | disarm | Disarm the drone motors | none | none |
        | takeoff | Take off to altitude | altitude | none |
        | land | Land at current position | none | none |
        | goto | Fly to specific position | x, y, z, speed | none |
        | rtl | Return to launch/home | none | altitude, speed |
        | hover | Hover at current position | none | duration |
        | emergency | Emergency stop - immediate landing | none | none |
        | status | Get current drone status | none | none |
        | orbit | Orbit around a point | center_x, center_y, radius | altitude, speed, laps |
        | survey | Survey/scan an area | width, height | altitude, speed, overlap |
        | patrol | Patrol between points | points (array) | altitude, speed, loops |
        | follow | Follow a target | target_x, target_y | altitude, distance, speed |
        
        ═══════════════════════════════════════════════════════════════════════════════
        COORDINATE SYSTEM
        ═══════════════════════════════════════════════════════════════════════════════
        
        Reference: Home position is (0, 0, 0) - typically Tel Aviv center
        
        AXES:
        - X axis: East-West direction
          - Positive X (+) = East (מזרח)
          - Negative X (-) = West (מערב)
        
        - Y axis: North-South direction
          - Positive Y (+) = North (צפון)
          - Negative Y (-) = South (דרום)
        
        - Z axis: Altitude above ground
          - Always positive
          - Measured in meters AGL (Above Ground Level)
        
        UNITS: All distances in METERS, all speeds in METERS PER SECOND (m/s)
        
        CONVERSIONS:
        - 1 kilometer = 1000 meters
        - 1 m/s ≈ 3.6 km/h
        - 10 m/s ≈ 36 km/h
        - 20 m/s ≈ 72 km/h
        - 50 m/s ≈ 180 km/h
        
        ═══════════════════════════════════════════════════════════════════════════════
        KNOWN LOCATIONS DATABASE
        ═══════════════════════════════════════════════════════════════════════════════
        
        All coordinates are in meters relative to Tel Aviv center (0, 0)
        
        GREATER TEL AVIV (גוש דן):
        | Location (EN) | Location (HE) | X (meters) | Y (meters) |
        |---------------|---------------|------------|------------|
        | Tel Aviv Center | מרכז תל אביב | 0 | 0 |
        | Tel Aviv North | צפון תל אביב | 0 | 3000 |
        | Tel Aviv South | דרום תל אביב | 0 | -2000 |
        | Jaffa | יפו | -1500 | -2500 |
        | Ramat Gan | רמת גן | 3500 | 2000 |
        | Givatayim | גבעתיים | 2500 | 1500 |
        | Bnei Brak | בני ברק | 4000 | 3000 |
        | Petah Tikva | פתח תקווה | 6000 | 5000 |
        | Holon | חולון | -1000 | -5000 |
        | Bat Yam | בת ים | -2000 | -4000 |
        | Rishon LeZion | ראשון לציון | -3000 | -8000 |
        | Ramat HaSharon | רמת השרון | 1000 | 6000 |
        | Herzliya | הרצליה | -2000 | 8000 |
        | Kfar Saba | כפר סבא | 3000 | 15000 |
        | Raanana | רעננה | 0 | 12000 |
        | Hod HaSharon | הוד השרון | 2000 | 13000 |
        | Lod | לוד | 8000 | -5000 |
        | Ramla | רמלה | 6000 | -8000 |
        | Rehovot | רחובות | 2000 | -15000 |
        | Nes Ziona | נס ציונה | 0 | -12000 |
        | Modiin | מודיעין | 20000 | -10000 |
        
        COASTAL CITIES (ערי החוף):
        | Location (EN) | Location (HE) | X (meters) | Y (meters) |
        |---------------|---------------|------------|------------|
        | Netanya | נתניה | -3000 | 25000 |
        | Hadera | חדרה | -5000 | 40000 |
        | Caesarea | קיסריה | -6000 | 45000 |
        | Haifa | חיפה | -10000 | 80000 |
        | Acre / Akko | עכו | -12000 | 90000 |
        | Nahariya | נהריה | -15000 | 100000 |
        | Ashdod | אשדוד | -8000 | -25000 |
        | Ashkelon | אשקלון | -12000 | -45000 |
        
        INLAND CITIES (ערי הפנים):
        | Location (EN) | Location (HE) | X (meters) | Y (meters) |
        |---------------|---------------|------------|------------|
        | Jerusalem | ירושלים | 55000 | -15000 |
        | Bethlehem | בית לחם | 55000 | -25000 |
        | Ramallah | רמאללה | 50000 | 0 |
        | Nablus | שכם | 45000 | 30000 |
        | Nazareth | נצרת | 20000 | 80000 |
        | Tiberias | טבריה | 45000 | 85000 |
        | Safed / Tzfat | צפת | 35000 | 100000 |
        
        SOUTHERN ISRAEL (דרום):
        | Location (EN) | Location (HE) | X (meters) | Y (meters) |
        |---------------|---------------|------------|------------|
        | Beer Sheva | באר שבע | 30000 | -80000 |
        | Dimona | דימונה | 50000 | -100000 |
        | Eilat | אילת | 100000 | -250000 |
        | Dead Sea | ים המלח | 70000 | -60000 |
        | Mitzpe Ramon | מצפה רמון | 50000 | -150000 |
        
        LANDMARKS & POIs:
        | Location (EN) | Location (HE) | X (meters) | Y (meters) |
        |---------------|---------------|------------|------------|
        | Ben Gurion Airport | נתב"ג | 10000 | -3000 |
        | Azrieli Towers | מגדלי עזריאלי | 1000 | 500 |
        | Dizengoff Center | דיזנגוף סנטר | -500 | 1500 |
        | Yarkon Park | פארק הירקון | 0 | 4000 |
        | Tel Aviv Port | נמל תל אביב | -1000 | 4500 |
        | Reading Power Station | תחנת רידינג | -2000 | 3000 |
        | Diamond Exchange | הבורסה ליהלומים | 3000 | 1500 |
        | Sheba Hospital | בית חולים שיבא | 4500 | 2500 |
        | Ichilov Hospital | איכילוב | 500 | 1000 |
        | HaBima Theater | תיאטרון הבימה | 500 | 500 |
        | Sarona Market | שרונה | 1500 | 0 |
        
        ═══════════════════════════════════════════════════════════════════════════════
        DIRECTION HANDLING
        ═══════════════════════════════════════════════════════════════════════════════
        
        When user specifies DIRECTION + DISTANCE, calculate from current position:
        
        | Direction (EN) | Direction (HE) | X Change | Y Change |
        |----------------|----------------|----------|----------|
        | North | צפון / צפונה | 0 | +distance |
        | South | דרום / דרומה | 0 | -distance |
        | East | מזרח / מזרחה | +distance | 0 |
        | West | מערב / מערבה | -distance | 0 |
        | Northeast | צפון-מזרח | +distance*0.7 | +distance*0.7 |
        | Northwest | צפון-מערב | -distance*0.7 | +distance*0.7 |
        | Southeast | דרום-מזרח | +distance*0.7 | -distance*0.7 |
        | Southwest | דרום-מערב | -distance*0.7 | -distance*0.7 |
        
        Examples:
        - "fly north 500m" from (100, 200) → new position (100, 700)
        - "fly east 1km" from (0, 0) → new position (1000, 0)
        - "טוס דרומה 2 ק"מ" from (500, 1000) → new position (500, -1000)
        
        ═══════════════════════════════════════════════════════════════════════════════
        DEFAULT VALUES
        ═══════════════════════════════════════════════════════════════════════════════
        
        Use these defaults when user doesn't specify:
        
        | Parameter | Default Value | Min | Max | Notes |
        |-----------|---------------|-----|-----|-------|
        | altitude (z) | 50 m | 10 m | 500 m | Safe cruising altitude |
        | speed | 15 m/s | 1 m/s | 70 m/s | ~54 km/h |
        | orbit radius | 50 m | 10 m | 500 m | |
        | orbit laps | 1 | 1 | 10 | |
        | survey overlap | 30% | 10% | 80% | |
        | hover duration | 10 s | 1 s | 300 s | |
        
        SPEED GUIDELINES:
        - Slow/careful: 5-10 m/s (18-36 km/h)
        - Normal: 10-20 m/s (36-72 km/h)
        - Fast: 20-40 m/s (72-144 km/h)
        - Very fast: 40-70 m/s (144-252 km/h)
        
        Hebrew speed terms:
        - לאט / איטי = slow = 8 m/s
        - רגיל / נורמלי = normal = 15 m/s
        - מהר / מהיר = fast = 30 m/s
        - מאוד מהר / במהירות מרבית = very fast = 50 m/s
        
        ═══════════════════════════════════════════════════════════════════════════════
        OUTPUT FORMAT
        ═══════════════════════════════════════════════════════════════════════════════
        
        ALWAYS respond with ONLY valid JSON in this exact format:
        
        {
            "command": "command_name",
            "params": {
                "param1": value1,
                "param2": value2
            },
            "confidence": 0.0 to 1.0,
            "response": "Human-readable response message"
        }
        
        CONFIDENCE LEVELS:
        - 0.95-1.0: Clear, unambiguous command with explicit values
        - 0.85-0.94: Clear command with some inferred values
        - 0.70-0.84: Understood intent but used defaults/estimates
        - 0.50-0.69: Uncertain, best guess
        - Below 0.50: Could not understand, return command="unknown"
        
        ═══════════════════════════════════════════════════════════════════════════════
        IMPORTANT RULES
        ═══════════════════════════════════════════════════════════════════════════════
        
        1. ALWAYS return valid JSON - no extra text, no markdown, no explanations outside JSON
        2. For "goto" command - MUST include all 4 params: x, y, z, speed
        3. For unknown locations - estimate based on nearby known locations or direction
        4. Never return empty params for goto - always calculate coordinates
        5. Understand both Hebrew and English commands
        6. Parse numbers in both formats: "50" and "חמישים"
        7. Handle units: meters (מטר), kilometers (ק"מ/קילומטר), feet (רגל)
        8. If command is unclear, set confidence < 0.5 and command = "unknown"
        9. Response message should be in the same language as the user's input
        10. For safety-critical commands (emergency, land), always high confidence if detected
        
        ═══════════════════════════════════════════════════════════════════════════════
        EXAMPLES
        ═══════════════════════════════════════════════════════════════════════════════
        
        Example 1 - Explicit coordinates:
        User: "fly to position 500, 800 at altitude 100 meters speed 25"
        Response: {"command": "goto", "params": {"x": 500, "y": 800, "z": 100, "speed": 25}, "confidence": 0.98, "response": "Flying to (500, 800) at 100m altitude, speed 25 m/s"}
        
        Example 2 - City name:
        User: "fly to Jerusalem at 300m altitude speed 60"
        Response: {"command": "goto", "params": {"x": 55000, "y": -15000, "z": 300, "speed": 60}, "confidence": 0.90, "response": "Flying to Jerusalem at 300m altitude, speed 60 m/s"}
        
        Example 3 - Hebrew city:
        User: "טוס להרצליה בגובה 150 מטר במהירות 40"
        Response: {"command": "goto", "params": {"x": -2000, "y": 8000, "z": 150, "speed": 40}, "confidence": 0.90, "response": "טס להרצליה בגובה 150 מטר, מהירות 40 מ/ש"}
        
        Example 4 - Direction based:
        User: "fly north 2 kilometers"
        Response: {"command": "goto", "params": {"x": 0, "y": 2000, "z": 50, "speed": 15}, "confidence": 0.95, "response": "Flying north 2km at 50m altitude"}
        
        Example 5 - Hebrew direction:
        User: "עוף מזרחה 500 מטר מהר"
        Response: {"command": "goto", "params": {"x": 500, "y": 0, "z": 50, "speed": 30}, "confidence": 0.92, "response": "טס מזרחה 500 מטר במהירות 30 מ/ש"}
        
        Example 6 - Takeoff:
        User: "takeoff to 80 meters"
        Response: {"command": "takeoff", "params": {"altitude": 80}, "confidence": 0.98, "response": "Taking off to 80 meters"}
        
        Example 7 - Hebrew takeoff:
        User: "המרא לגובה 50 מטר"
        Response: {"command": "takeoff", "params": {"altitude": 50}, "confidence": 0.98, "response": "ממריא לגובה 50 מטר"}
        
        Example 8 - Land:
        User: "land now"
        Response: {"command": "land", "params": {}, "confidence": 0.98, "response": "Landing immediately"}
        
        Example 9 - RTL:
        User: "חזור הביתה"
        Response: {"command": "rtl", "params": {}, "confidence": 0.95, "response": "חוזר לנקודת ההמראה"}
        
        Example 10 - Orbit:
        User: "orbit around Azrieli towers radius 200m at 150m altitude"
        Response: {"command": "orbit", "params": {"center_x": 1000, "center_y": 500, "radius": 200, "altitude": 150, "speed": 15}, "confidence": 0.88, "response": "Orbiting Azrieli Towers, radius 200m at 150m altitude"}
        
        Example 11 - Status:
        User: "מה המצב?"
        Response: {"command": "status", "params": {}, "confidence": 0.95, "response": "מציג סטטוס הרחפן"}
        
        Example 12 - Emergency:
        User: "עצור מיד!"
        Response: {"command": "emergency", "params": {}, "confidence": 0.99, "response": "עצירת חירום מופעלת!"}
        
        Example 13 - Landmark:
        User: "fly to Ben Gurion Airport at 200m"
        Response: {"command": "goto", "params": {"x": 10000, "y": -3000, "z": 200, "speed": 15}, "confidence": 0.88, "response": "Flying to Ben Gurion Airport at 200m altitude"}
        
        Example 14 - Survey:
        User: "survey 500x500 meter area at 80m altitude"
        Response: {"command": "survey", "params": {"width": 500, "height": 500, "altitude": 80, "speed": 10}, "confidence": 0.92, "response": "Starting survey mission: 500x500m area at 80m altitude"}
        
        Example 15 - Unknown/Unclear:
        User: "do something cool"
        Response: {"command": "unknown", "params": {}, "confidence": 0.2, "response": "I didn't understand. Try: takeoff, land, fly to [location], status, orbit, survey"}
        
        ═══════════════════════════════════════════════════════════════════════════════
        RESPOND WITH JSON ONLY - NO OTHER TEXT!
        ═══════════════════════════════════════════════════════════════════════════════
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CommandInterpreter(AnthropicClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Interpret a natural language command.
    /// </summary>
    public async Task<DroneCommand> InterpretAsync(string userInput)
    {
        try
        {
            var response = await _client.SendMessageAsync(userInput, SystemPrompt);
            Console.WriteLine($"[Interpreter] Raw AI response: {response}");
            return ParseCommand(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Interpreter] Error: {ex.Message}");
            return DroneCommand.Unknown($"API Error: {ex.Message}");
        }
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
            
            ═══════════════════════════════════════════════════════════════════════════════
            CURRENT DRONE STATE (use for relative calculations)
            ═══════════════════════════════════════════════════════════════════════════════
            
            | Property | Value |
            |----------|-------|
            | Status | {state.Status} |
            | Current X | {state.Position.X:F0} meters |
            | Current Y | {state.Position.Y:F0} meters |
            | Current Z (Altitude) | {state.Position.Z:F0} meters |
            | Altitude AGL | {state.AltitudeAGL:F0} meters |
            | Ground Speed | {state.GroundSpeed:F1} m/s |
            | Heading | {state.Heading:F0}° |
            | Battery | {state.BatteryPercent:F0}% |
            | Distance from Home | {state.DistanceFromHome:F0} meters |
            | Current Mission | {currentMission ?? "None"} |
            
            IMPORTANT: For relative directions (north/south/east/west), calculate from CURRENT position!
            Example: If current position is (1000, 2000) and user says "fly north 500m", 
                     new position should be (1000, 2500)
            
            ═══════════════════════════════════════════════════════════════════════════════
            RESPOND WITH JSON ONLY!
            ═══════════════════════════════════════════════════════════════════════════════
            """;

        try
        {
            var response = await _client.SendMessageAsync(userInput, contextPrompt);
            Console.WriteLine($"[Interpreter] Raw AI response: {response}");
            return ParseCommand(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Interpreter] Error: {ex.Message}");
            return DroneCommand.Unknown($"API Error: {ex.Message}");
        }
    }

    private DroneCommand ParseCommand(string jsonResponse)
    {
        try
        {
            var jsonStart = jsonResponse.IndexOf('{');
            var jsonEnd = jsonResponse.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                Console.WriteLine($"[Interpreter] No JSON found in response");
                return DroneCommand.Unknown("No JSON in response");
            }

            var json = jsonResponse[jsonStart..(jsonEnd + 1)];
            Console.WriteLine($"[Interpreter] Extracted JSON: {json}");

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var command = new DroneCommand
            {
                Command = root.TryGetProperty("command", out var cmdProp)
                    ? cmdProp.GetString() ?? "unknown"
                    : "unknown",
                Confidence = root.TryGetProperty("confidence", out var confProp)
                    ? confProp.GetDouble()
                    : 0.0,
                Response = root.TryGetProperty("response", out var respProp)
                    ? respProp.GetString() ?? ""
                    : ""
            };

            if (root.TryGetProperty("params", out var paramsProp) && paramsProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in paramsProp.EnumerateObject())
                {
                    object value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                    command.Params[prop.Name] = value;
                }
            }

            Console.WriteLine($"[Interpreter] Parsed: cmd={command.Command}, confidence={command.Confidence:F2}, params=[{string.Join(", ", command.Params.Select(p => $"{p.Key}={p.Value}"))}]");

            return command;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Interpreter] Parse error: {ex.Message}");
            return DroneCommand.Unknown($"Parse failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Parsed drone command from AI.
/// </summary>
public class DroneCommand
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    public static DroneCommand Unknown(string reason) => new()
    {
        Command = "unknown",
        Confidence = 0,
        Response = reason
    };

    // Helper methods for getting typed parameters
    public double GetDouble(string key, double defaultValue = 0)
    {
        if (!Params.TryGetValue(key, out var val))
            return defaultValue;

        return val switch
        {
            double d => d,
            long l => l,
            int i => i,
            float f => f,
            string s when double.TryParse(s, out var parsed) => parsed,
            JsonElement je when je.ValueKind == JsonValueKind.Number => je.GetDouble(),
            _ => defaultValue
        };
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (!Params.TryGetValue(key, out var val))
            return defaultValue;

        return val switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string s when int.TryParse(s, out var parsed) => parsed,
            JsonElement je when je.ValueKind == JsonValueKind.Number => je.GetInt32(),
            _ => defaultValue
        };
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (!Params.TryGetValue(key, out var val))
            return defaultValue;

        return val?.ToString() ?? defaultValue;
    }
}