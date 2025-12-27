namespace GIS3DEngine.Application.Dtos.Responses;

public record ChatResponseDto
{
    public string Response { get; init; } = string.Empty;
    public bool HasCommand { get; init; }
    public string? CommandType { get; init; }
    public bool CommandExecuted { get; init; }
    public string? CommandResult { get; init; }
}