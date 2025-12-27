namespace GIS3DEngine.Application.Dtos.Requests;

public record DeliveryMissionRequestDto
{
    public string DroneId { get; init; } = string.Empty;
    public string? Name { get; init; }
    public double PickupX { get; init; }
    public double PickupY { get; init; }
    public double DeliveryX { get; init; }
    public double DeliveryY { get; init; }
    public double Altitude { get; init; } = 50;
    public double Speed { get; init; } = 10;
    public double HoverTimeAtPickup { get; init; } = 10;
    public double HoverTimeAtDelivery { get; init; } = 10;
}