namespace GIS3DEngine.Drones.Core;

/// <summary>
/// Physical and performance specifications of a drone.
/// </summary>
public class DroneSpecifications
{
    #region Identity

    /// <summary>Drone type category.</summary>
    public DroneType Type { get; set; } = DroneType.Quadcopter;

    /// <summary>Model name.</summary>
    public string Model { get; set; } = "Generic Quadcopter";

    /// <summary>Manufacturer.</summary>
    public string Manufacturer { get; set; } = "Unknown";

    #endregion

    #region Physical Properties

    /// <summary>Weight in kilograms.</summary>
    public double WeightKg { get; set; } = 2.5;

    /// <summary>Maximum payload in kilograms.</summary>
    public double MaxPayloadKg { get; set; } = 1.0;

    /// <summary>Wingspan/diameter in meters.</summary>
    public double SizeMeters { get; set; } = 0.5;

    #endregion

    #region Performance

    /// <summary>Maximum speed in m/s.</summary>
    public double MaxSpeedMs { get; set; } = 90.0;

    /// <summary>Maximum climb rate in m/s.</summary>
    public double MaxClimbRateMs { get; set; } = 5.0;

    /// <summary>Maximum descent rate in m/s.</summary>
    public double MaxDescentRateMs { get; set; } = 3.0;

    /// <summary>Maximum altitude in meters AGL.</summary>
    public double MaxAltitudeM { get; set; } = 500.0;

    /// <summary>Minimum safe altitude in meters.</summary>
    public double MinSafeAltitudeM { get; set; } = 10.0;

    #endregion

    #region Battery

    /// <summary>Battery capacity in mAh.</summary>
    public int BatteryCapacityMah { get; set; } = 5000;

    /// <summary>Maximum flight time in minutes.</summary>
    public double MaxFlightTimeMinutes { get; set; } = 30.0;

    /// <summary>Battery voltage.</summary>
    public double BatteryVoltage { get; set; } = 22.2;

    #endregion

    #region Communication

    /// <summary>Maximum communication range in meters.</summary>
    public double MaxRangeM { get; set; } = 5000.0;

    /// <summary>Telemetry update rate in Hz.</summary>
    public int TelemetryRateHz { get; set; } = 10;

    #endregion

    #region Sensors

    /// <summary>List of sensors installed.</summary>
    public List<SensorType> Sensors { get; set; } = new()
    {
        SensorType.GPS,
        SensorType.IMU,
        SensorType.Barometer,
        SensorType.Magnetometer
    };

    #endregion

    #region Factory Methods - Common Drones

    /// <summary>DJI Mavic 3 specifications.</summary>
    public static DroneSpecifications DJIMavic3 => new()
    {
        Type = DroneType.Quadcopter,
        Model = "Mavic 3",
        Manufacturer = "DJI",
        WeightKg = 0.895,
        MaxPayloadKg = 0.2,
        SizeMeters = 0.347,
        MaxSpeedMs = 21.0,
        MaxClimbRateMs = 8.0,
        MaxDescentRateMs = 6.0,
        MaxAltitudeM = 6000.0,
        BatteryCapacityMah = 5000,
        MaxFlightTimeMinutes = 46.0,
        BatteryVoltage = 17.6,
        MaxRangeM = 15000.0,
        TelemetryRateHz = 10,
        Sensors = new() { SensorType.GPS, SensorType.IMU, SensorType.Camera, SensorType.Rangefinder }
    };

    /// <summary>DJI Matrice 300 RTK specifications.</summary>
    public static DroneSpecifications DJIMatrice300 => new()
    {
        Type = DroneType.Quadcopter,
        Model = "Matrice 300 RTK",
        Manufacturer = "DJI",
        WeightKg = 6.3,
        MaxPayloadKg = 2.7,
        SizeMeters = 0.81,
        MaxSpeedMs = 23.0,
        MaxClimbRateMs = 6.0,
        MaxDescentRateMs = 5.0,
        MaxAltitudeM = 7000.0,
        BatteryCapacityMah = 5935,
        MaxFlightTimeMinutes = 55.0,
        BatteryVoltage = 52.8,
        MaxRangeM = 15000.0,
        TelemetryRateHz = 10,
        Sensors = new() { SensorType.GPS, SensorType.IMU, SensorType.Camera, SensorType.LiDAR, SensorType.ThermalCamera }
    };

    /// <summary>Fixed-wing survey drone specifications.</summary>
    public static DroneSpecifications SurveyDrone => new()
    {
        Type = DroneType.FixedWing,
        Model = "Survey Wing",
        Manufacturer = "Generic",
        WeightKg = 4.0,
        MaxPayloadKg = 1.5,
        SizeMeters = 2.0,
        MaxSpeedMs = 30.0,
        MaxClimbRateMs = 4.0,
        MaxDescentRateMs = 3.0,
        MaxAltitudeM = 3000.0,
        BatteryCapacityMah = 16000,
        MaxFlightTimeMinutes = 90.0,
        BatteryVoltage = 22.2,
        MaxRangeM = 50000.0,
        TelemetryRateHz = 5,
        Sensors = new() { SensorType.GPS, SensorType.IMU, SensorType.Camera, SensorType.Multispectral }
    };

    /// <summary>Heavy-lift delivery drone specifications.</summary>
    public static DroneSpecifications DeliveryDrone => new()
    {
        Type = DroneType.Hexacopter,
        Model = "Delivery Hex",
        Manufacturer = "Generic",
        WeightKg = 8.0,
        MaxPayloadKg = 5.0,
        SizeMeters = 1.2,
        MaxSpeedMs = 15.0,
        MaxClimbRateMs = 3.0,
        MaxDescentRateMs = 2.0,
        MaxAltitudeM = 120.0,
        BatteryCapacityMah = 22000,
        MaxFlightTimeMinutes = 25.0,
        BatteryVoltage = 44.4,
        MaxRangeM = 10000.0,
        TelemetryRateHz = 10,
        Sensors = new() { SensorType.GPS, SensorType.IMU, SensorType.Rangefinder, SensorType.Ultrasonic }
    };

    /// <summary>Racing/inspection quadcopter specifications.</summary>
    public static DroneSpecifications RacingDrone => new()
    {
        Type = DroneType.Quadcopter,
        Model = "Racing Quad",
        Manufacturer = "Generic",
        WeightKg = 0.5,
        MaxPayloadKg = 0.1,
        SizeMeters = 0.25,
        MaxSpeedMs = 40.0,
        MaxClimbRateMs = 15.0,
        MaxDescentRateMs = 10.0,
        MaxAltitudeM = 500.0,
        BatteryCapacityMah = 1500,
        MaxFlightTimeMinutes = 8.0,
        BatteryVoltage = 14.8,
        MaxRangeM = 1000.0,
        TelemetryRateHz = 50,
        Sensors = new() { SensorType.IMU, SensorType.Camera }
    };

    #endregion
}