namespace GIS3DEngine.WebApi.Dtos;

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public bool HasCommand { get; set; }
    public string? CommandType { get; set; }
    public bool CommandExecuted { get; set; }
    public string? CommandResult { get; set; }
}

