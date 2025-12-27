namespace GIS3DEngine.Application.Dtos.Requests;

public record ChatRequestDto
{
    public string Message { get; init; } = string.Empty;
    public string? DroneId { get; init; }
}