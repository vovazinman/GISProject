namespace GIS3DEngine.Application.Dtos.Requests;

public record SurveyMissionRequestDto
{
    public string DroneId { get; init; } = string.Empty;
    public string? Name { get; init; }
    public double OriginX { get; init; } = 0;
    public double OriginY { get; init; } = 0;
    public double Width { get; init; } = 100;
    public double Height { get; init; } = 100;
    public double Altitude { get; init; } = 50;
    public double Speed { get; init; } = 10;
    public string Pattern { get; init; } = "Lawnmower";
    public double LineSpacing { get; init; } = 20;
}