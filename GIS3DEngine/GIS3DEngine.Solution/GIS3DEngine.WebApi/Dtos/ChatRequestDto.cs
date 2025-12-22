namespace GIS3DEngine.WebApi.Dtos;

public class ChatRequestDto
{
    /// <summary>
    /// User input message (natural language)
    /// </summary>
    public string Message { get; set; } = string.Empty;
    public string? DroneId { get; set; }
}
