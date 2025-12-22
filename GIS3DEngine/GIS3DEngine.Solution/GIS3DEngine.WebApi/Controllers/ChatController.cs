using GIS3DEngine.Drones.AI;
using GIS3DEngine.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GIS3DEngine.WebApi.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly DroneAssistant _assistant;

    public ChatController(DroneAssistant assistant)
    {
        _assistant = assistant;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(ChatRequestDto request)
    {
        var response = await _assistant.SimpleChatAsync(request.Message);
        return Ok(new { reply = response });
    }
}

