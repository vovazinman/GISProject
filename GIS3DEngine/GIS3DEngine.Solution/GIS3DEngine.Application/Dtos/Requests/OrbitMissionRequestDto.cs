namespace GIS3DEngine.Application.Dtos.Requests;

public record OrbitMissionRequestDto
{
    public string DroneId { get; init; } = string.Empty;
    public string? Name { get; init; }
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double Radius { get; init; } = 50;
    public double Altitude { get; init; } = 50;
    public double Speed { get; init; } = 10;
    public int Orbits { get; init; } = 1;
    public bool Clockwise { get; init; } = true;
}