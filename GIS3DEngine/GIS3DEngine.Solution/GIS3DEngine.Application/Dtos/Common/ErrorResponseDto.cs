namespace GIS3DEngine.Application.Dtos.Common;

public record ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;
    public string? Details { get; init; }
    public int StatusCode { get; init; }

    public static ErrorResponseDto NotFound(string message) => new()
    {
        Error = message,
        StatusCode = 404
    };

    public static ErrorResponseDto BadRequest(string message, string? details = null) => new()
    {
        Error = message,
        Details = details,
        StatusCode = 400
    };
}