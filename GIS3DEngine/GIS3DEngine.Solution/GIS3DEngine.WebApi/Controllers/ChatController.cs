using GIS3DEngine.Drones.AI;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.WebApi.Dtos;
using GIS3DEngine.WebApi.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.Intrinsics.X86;

namespace GIS3DEngine.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly DroneFleetManager _fleet;
    private readonly IHubContext<DroneHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        DroneFleetManager fleet,
        IHubContext<DroneHub> hubContext,
        IConfiguration config,
        ILogger<ChatController> logger)
    {
        _fleet = fleet;
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Send chat message to AI assistant
    /// POST /api/chat
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        // Get API key from configuration
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "API Key not configured",
                Details = "Add Anthropic:ApiKey to appsettings.json",
                StatusCode = 400
            });
        }

        try
        {
            // Get drone (specified or first available)
            var drone = GetDrone(request.DroneId);

            // Create assistant and set active drone
            var assistant = new DroneAssistant(apiKey);
            if (drone != null)
            {
                assistant.SetActiveDrone(drone);
            }

            // Send message to AI
            var response = await assistant.ChatAsync(request.Message);

            // Broadcast messages via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "User", request.Message);
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "AI", response.Text);

            _logger.LogInformation("Chat: {Message} -> {Response}",
                request.Message, response.Text.Substring(0, Math.Min(50, response.Text.Length)));

            return Ok(new ChatResponse
            {
                Response = response.Text,
                HasCommand = response.HasCommand,
                CommandType = response.Command?.Command,
                CommandExecuted = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat error for message: {Message}", request.Message);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Chat failed",
                Details = ex.Message,
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Send command via natural language and execute it
    /// POST /api/chat/command
    /// </summary>
    [HttpPost("command")]
    public async Task<ActionResult<ChatResponse>> Command([FromBody] ChatRequest request)
    {
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "API Key not configured",
                StatusCode = 400
            });
        }

        try
        {
            var drone = GetDrone(request.DroneId);
            if (drone == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "No drone available",
                    Details = "Create a drone first or specify a valid DroneId",
                    StatusCode = 400
                });
            }

            var assistant = new DroneAssistant(apiKey);
            assistant.SetActiveDrone(drone);

            // Process command - this interprets AND executes
            var result = await assistant.ProcessCommandAsync(request.Message);

            // Broadcast updates
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "User", request.Message);

            if (result.IsSuccess)
            {
                await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", "AI",
                    $"✅ {result.Message}");
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", "AI",
                    $"❌ {result.Message}");
            }

            return Ok(new ChatResponse
            {
                Response = result.AiResponse ?? result.Message,
                HasCommand = true,
                CommandExecuted = result.IsSuccess,
                CommandResult = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command error: {Message}", request.Message);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Command failed",
                Details = ex.Message,
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Stream chat response (Server-Sent Events)
    /// POST /api/chat/stream
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamChat([FromBody] ChatRequest request)
    {
        // Set headers for SSE (Server-Sent Events)
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
        {
            await WriteSSE(new { error = "API Key not configured" });
            return;
        }

        try
        {
            var drone = GetDrone(request.DroneId);
            var assistant = new DroneAssistant(apiKey);

            if (drone != null)
            {
                assistant.SetActiveDrone(drone);
            }

            // Stream response chunks
            await foreach (var chunk in assistant.StreamChatAsync(request.Message))
            {
                await WriteSSE(new { chunk });

                // Also broadcast via SignalR for other clients
                await _hubContext.Clients.All.SendAsync("ChatChunk", chunk);
            }

            await WriteSSE(new { done = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stream chat error");
            await WriteSSE(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test API connection
    /// GET /api/chat/test
    /// </summary>
    [HttpGet("test")]
    public async Task<ActionResult<object>> TestConnection()
    {
        var apiKey = _config["Anthropic:ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
        {
            return Ok(new
            {
                status = "Not Configured",
                configured = false,
                message = "Add your API key to appsettings.json"
            });
        }

        try
        {
            // Quick test call
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await client.PostAsJsonAsync(
                "https://api.anthropic.com/v1/messages",
                new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 50,
                    messages = new[] { new { role = "user", content = "Say 'OK'" } }
                });

            if (response.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    status = "Connected",
                    configured = true,
                    message = "API is working!"
                });
            }

            var error = await response.Content.ReadAsStringAsync();
            return Ok(new
            {
                status = "Error",
                configured = true,
                statusCode = response.StatusCode,
                error
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                status = "Error",
                configured = true,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get conversation history (if implemented in DroneAssistant)
    /// GET /api/chat/history
    /// </summary>
    [HttpGet("history")]
    public ActionResult<object> GetHistory()
    {
        // TODO: Implement if DroneAssistant stores history
        return Ok(new
        {
            message = "History not implemented yet",
            hint = "DroneAssistant could store conversation history"
        });
    }

    #region Helpers

    /// <summary>
    /// Get drone by ID or return first available
    /// </summary>
    private Drone? GetDrone(string? droneId)
    {
        if (!string.IsNullOrEmpty(droneId))
        {
            return _fleet.GetDrone(droneId);
        }

        return _fleet.GetAllDrones().FirstOrDefault();
    }

    /// <summary>
    /// Write Server-Sent Event
    /// </summary>
    private async Task WriteSSE(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    #endregion
}

/*+++++++++++++++++++++++++++++++++++++++++++++++++++++
* ChatController Endpoints Overview
#   
#   | Method | Route | תיאור |
#   |--------|-------|--------|
#   | `POST` | `/api/chat` | שליחת הודעה ל-AI(לא מבצע פקודות) |
#   | `POST` | `/api/chat/command` | שליחת פקודה בשפה טבעית(מבצע!) |
#   | `POST` | `/api/chat/stream` | Streaming response(SSE) |
#   | `GET` | `/api/chat/test` | בדיקת חיבור ל-API |
#   | `GET` | `/api/chat/history` | היסטוריית שיחה(TODO) |
#   | `GET` | `/api/chat/commands` | רשימת פקודות זמינות |  
#   
#   ---
#   
#   ##  הסבר על ההבדלים:
#   
#   ### `/api/chat` vs `/api/chat/command`
#   ```
#   /api/chat:
#   ┌─────────────────────────────────────────────────────┐
#   │  User: "מה המצב של הרחפן?"                          │
#   │  AI: "הרחפן במצב Ready, סוללה 85%, גובה 0 מטר"      │
#   │  → רק מחזיר תשובה, לא מבצע כלום                      │
#   └─────────────────────────────────────────────────────┘
#   
#   /api/chat/command:
#   ┌─────────────────────────────────────────────────────┐
#   │  User: "המרא לגובה 50 מטר"                          │
#   │  AI: מפרסר → { command: "takeoff", altitude: 50 }   │
#   │  → מבצע drone.Takeoff(50)                           │
#   │  → מחזיר תוצאה + מצב מעודכן                          │
#   └─────────────────────────────────────────────────────┘
#   ```
#   
#   ### Streaming (SSE)
#   ```
#   Client                          Server
#     │                               │
#     │──POST /api/chat/stream───────>│
#     │                               │
#     │<──data: {"chunk": "הר"}───────│
#     │<──data: {"chunk": "חפן "}─────│
#     │<──data: {"chunk": "מו"}───────│
#     │<──data: {"chunk": "כן"}───────│
#     │<──data: {"done": true}────────│
#     │                               │
# 
**********************************************
#   
*/
