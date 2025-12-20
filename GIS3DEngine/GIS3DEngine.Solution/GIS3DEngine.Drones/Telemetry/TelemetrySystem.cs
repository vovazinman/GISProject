using System.Collections.Concurrent;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;

namespace GIS3DEngine.Drones.Telemetry;

/// <summary>
/// Telemetry data packet from a drone.
/// </summary>
public class TelemetryPacket
{
    public string DroneId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long SequenceNumber { get; set; }

    // Position
    public Vector3D Position { get; set; }
    public GeoCoordinate? GeoPosition { get; set; }
    public double AltitudeAGL { get; set; }
    public double AltitudeMSL { get; set; }

    // Velocity
    public Vector3D Velocity { get; set; }
    public double GroundSpeed { get; set; }
    public double VerticalSpeed { get; set; }

    // Attitude
    public double Roll { get; set; }
    public double Pitch { get; set; }
    public double Yaw { get; set; }
    public double Heading { get; set; }

    // Status
    public DroneStatus Status { get; set; }
    public FlightMode FlightMode { get; set; }
    public bool IsArmed { get; set; }
    public bool IsFailsafe { get; set; }

    // Battery
    public double BatteryPercent { get; set; }
    public double BatteryVoltage { get; set; }
    public double BatteryCurrent { get; set; }
    public double RemainingFlightTimeSec { get; set; }

    // GPS
    public int GpsSatellites { get; set; }
    public int GpsFixQuality { get; set; }
    public double GpsHDOP { get; set; }

    // Navigation
    public double DistanceFromHome { get; set; }
    public double DistanceTraveled { get; set; }
    public int CurrentWaypointIndex { get; set; }
    public int TotalWaypoints { get; set; }
    public double MissionProgress { get; set; }

    // Signal
    public double SignalStrength { get; set; }
    public double RssiDbm { get; set; }

    // Sensors
    public Dictionary<string, double> SensorValues { get; set; } = new();

    /// <summary>
    /// Create telemetry packet from drone state.
    /// </summary>
    public static TelemetryPacket FromDroneState(string droneId, DroneState state, long sequenceNumber = 0)
    {
        return new TelemetryPacket
        {
            DroneId = droneId,
            Timestamp = state.Timestamp,
            SequenceNumber = sequenceNumber,
            Position = state.Position,
            GeoPosition = state.GeoPosition,
            AltitudeAGL = state.AltitudeAGL,
            AltitudeMSL = state.AltitudeMSL,
            Velocity = state.Velocity,
            GroundSpeed = state.GroundSpeed,
            VerticalSpeed = state.VerticalSpeed,
            Roll = state.Attitude.X,
            Pitch = state.Attitude.Y,
            Yaw = state.Attitude.Z,
            Heading = state.Heading,
            Status = state.Status,
            FlightMode = state.FlightMode,
            IsArmed = state.IsArmed,
            IsFailsafe = state.IsFailsafe,
            BatteryPercent = state.BatteryPercent,
            BatteryVoltage = state.BatteryVoltage,
            RemainingFlightTimeSec = state.RemainingFlightTimeSec,
            GpsSatellites = state.GpsSatellites,
            GpsFixQuality = state.GpsFixQuality,
            DistanceFromHome = state.DistanceFromHome,
            DistanceTraveled = state.DistanceTraveled,
            CurrentWaypointIndex = state.CurrentWaypointIndex,
            TotalWaypoints = state.TotalWaypoints,
            MissionProgress = state.TotalWaypoints > 0
                ? (double)state.CurrentWaypointIndex / state.TotalWaypoints * 100
                : 0,
            SignalStrength = state.SignalStrength
        };
    }
}

/// <summary>
/// Records and manages telemetry history for drones.
/// </summary>
public class TelemetryRecorder
{
    private readonly ConcurrentDictionary<string, TelemetryBuffer> _buffers = new();
    private readonly int _maxHistoryPerDrone;
    private long _sequenceCounter;

    public event EventHandler<TelemetryReceivedEventArgs>? TelemetryReceived;

    public TelemetryRecorder(int maxHistoryPerDrone = 1000)
    {
        _maxHistoryPerDrone = maxHistoryPerDrone;
    }

    /// <summary>
    /// Record a telemetry packet.
    /// </summary>
    public void Record(TelemetryPacket packet)
    {
        packet.SequenceNumber = Interlocked.Increment(ref _sequenceCounter);

        var buffer = _buffers.GetOrAdd(packet.DroneId, _ => new TelemetryBuffer(_maxHistoryPerDrone));
        buffer.Add(packet);

        TelemetryReceived?.Invoke(this, new TelemetryReceivedEventArgs(packet));
    }

    /// <summary>
    /// Record telemetry from drone state.
    /// </summary>
    public void RecordFromState(string droneId, DroneState state)
    {
        var packet = TelemetryPacket.FromDroneState(droneId, state);
        Record(packet);
    }

    /// <summary>
    /// Get latest telemetry for a drone.
    /// </summary>
    public TelemetryPacket? GetLatest(string droneId)
    {
        return _buffers.TryGetValue(droneId, out var buffer) ? buffer.GetLatest() : null;
    }

    /// <summary>
    /// Get telemetry history for a drone.
    /// </summary>
    public IReadOnlyList<TelemetryPacket> GetHistory(string droneId, int count = 100)
    {
        return _buffers.TryGetValue(droneId, out var buffer)
            ? buffer.GetHistory(count)
            : Array.Empty<TelemetryPacket>();
    }

    /// <summary>
    /// Get telemetry within time range.
    /// </summary>
    public IReadOnlyList<TelemetryPacket> GetHistoryInRange(string droneId, DateTime start, DateTime end)
    {
        return _buffers.TryGetValue(droneId, out var buffer)
            ? buffer.GetHistory().Where(p => p.Timestamp >= start && p.Timestamp <= end).ToList()
            : Array.Empty<TelemetryPacket>();
    }

    /// <summary>
    /// Get all tracked drone IDs.
    /// </summary>
    public IReadOnlyList<string> GetTrackedDrones() => _buffers.Keys.ToList();

    /// <summary>
    /// Clear history for a drone.
    /// </summary>
    public void ClearHistory(string droneId)
    {
        if (_buffers.TryGetValue(droneId, out var buffer))
            buffer.Clear();
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    public void ClearAll()
    {
        _buffers.Clear();
    }
}

/// <summary>
/// Circular buffer for telemetry storage.
/// </summary>
internal class TelemetryBuffer
{
    private readonly TelemetryPacket[] _buffer;
    private readonly object _lock = new();
    private int _head;
    private int _count;

    public TelemetryBuffer(int capacity)
    {
        _buffer = new TelemetryPacket[capacity];
    }

    public void Add(TelemetryPacket packet)
    {
        lock (_lock)
        {
            _buffer[_head] = packet;
            _head = (_head + 1) % _buffer.Length;
            _count = Math.Min(_count + 1, _buffer.Length);
        }
    }

    public TelemetryPacket? GetLatest()
    {
        lock (_lock)
        {
            if (_count == 0) return null;
            var index = (_head - 1 + _buffer.Length) % _buffer.Length;
            return _buffer[index];
        }
    }

    public IReadOnlyList<TelemetryPacket> GetHistory(int count = -1)
    {
        lock (_lock)
        {
            if (count < 0 || count > _count) count = _count;
            var result = new TelemetryPacket[count];

            for (int i = 0; i < count; i++)
            {
                var index = (_head - count + i + _buffer.Length) % _buffer.Length;
                result[i] = _buffer[index];
            }

            return result;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer);
            _head = 0;
            _count = 0;
        }
    }
}

/// <summary>
/// Event args for telemetry received.
/// </summary>
public class TelemetryReceivedEventArgs : EventArgs
{
    public TelemetryPacket Packet { get; }

    public TelemetryReceivedEventArgs(TelemetryPacket packet)
    {
        Packet = packet;
    }
}

/// <summary>
/// Analyzes telemetry for insights and alerts.
/// </summary>
public class TelemetryAnalyzer
{
    private readonly TelemetryRecorder _recorder;

    public event EventHandler<TelemetryAlertEventArgs>? AlertRaised;

    public TelemetryAnalyzer(TelemetryRecorder recorder)
    {
        _recorder = recorder;
        _recorder.TelemetryReceived += OnTelemetryReceived;
    }

    private void OnTelemetryReceived(object? sender, TelemetryReceivedEventArgs e)
    {
        var packet = e.Packet;

        // Battery alerts
        if (packet.BatteryPercent <= 10)
            RaiseAlert(packet.DroneId, AlertLevel.Critical, $"Critical battery: {packet.BatteryPercent:F1}%");
        else if (packet.BatteryPercent <= 20)
            RaiseAlert(packet.DroneId, AlertLevel.Warning, $"Low battery: {packet.BatteryPercent:F1}%");

        // GPS alerts
        if (packet.GpsFixQuality < 3)
            RaiseAlert(packet.DroneId, AlertLevel.Warning, $"Poor GPS quality: {packet.GpsFixQuality}");

        // Signal alerts
        if (packet.SignalStrength < 30)
            RaiseAlert(packet.DroneId, AlertLevel.Warning, $"Weak signal: {packet.SignalStrength:F1}%");

        // Failsafe
        if (packet.IsFailsafe)
            RaiseAlert(packet.DroneId, AlertLevel.Critical, "Failsafe mode active");
    }

    private void RaiseAlert(string droneId, AlertLevel level, string message)
    {
        AlertRaised?.Invoke(this, new TelemetryAlertEventArgs(droneId, level, message));
    }

    /// <summary>
    /// Calculate average speed over recent history.
    /// </summary>
    public double GetAverageSpeed(string droneId, int sampleCount = 10)
    {
        var history = _recorder.GetHistory(droneId, sampleCount);
        return history.Count > 0 ? history.Average(p => p.GroundSpeed) : 0;
    }

    /// <summary>
    /// Calculate battery consumption rate (% per minute).
    /// </summary>
    public double GetBatteryConsumptionRate(string droneId)
    {
        var history = _recorder.GetHistory(droneId, 60);
        if (history.Count < 2) return 0;

        var first = history.First();
        var last = history.Last();
        var timeDiffMinutes = (last.Timestamp - first.Timestamp).TotalMinutes;
        var batteryDiff = first.BatteryPercent - last.BatteryPercent;

        return timeDiffMinutes > 0 ? batteryDiff / timeDiffMinutes : 0;
    }

    /// <summary>
    /// Estimate remaining flight time based on consumption.
    /// </summary>
    public double EstimateRemainingMinutes(string droneId)
    {
        var latest = _recorder.GetLatest(droneId);
        if (latest == null) return 0;

        var consumptionRate = GetBatteryConsumptionRate(droneId);
        if (consumptionRate <= 0) return latest.RemainingFlightTimeSec / 60;

        return latest.BatteryPercent / consumptionRate;
    }

    /// <summary>
    /// Get flight path as list of positions.
    /// </summary>
    public IReadOnlyList<Vector3D> GetFlightPath(string droneId, int maxPoints = 500)
    {
        return _recorder.GetHistory(droneId, maxPoints)
            .Select(p => p.Position)
            .ToList();
    }
}

public enum AlertLevel
{
    Info,
    Warning,
    Critical
}

public class TelemetryAlertEventArgs : EventArgs
{
    public string DroneId { get; }
    public AlertLevel Level { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }

    public TelemetryAlertEventArgs(string droneId, AlertLevel level, string message)
    {
        DroneId = droneId;
        Level = level;
        Message = message;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Exports telemetry data to various formats.
/// </summary>
public class TelemetryExporter
{
    /// <summary>
    /// Export telemetry to CSV format.
    /// </summary>
    public string ExportToCsv(IReadOnlyList<TelemetryPacket> packets)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("DroneId,Timestamp,Lat,Lon,Alt,Speed,Heading,Battery,Status");

        foreach (var p in packets)
        {
            var lat = p.GeoPosition?.Latitude ?? 0;
            var lon = p.GeoPosition?.Longitude ?? 0;
            sb.AppendLine($"{p.DroneId},{p.Timestamp:O},{lat},{lon},{p.AltitudeAGL},{p.GroundSpeed:F2},{p.Heading:F2},{p.BatteryPercent:F1},{p.Status}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export flight path to GeoJSON.
    /// </summary>
    public string ExportToGeoJson(string droneId, IReadOnlyList<TelemetryPacket> packets)
    {
        var coordinates = packets
            .Where(p => p.GeoPosition != null)
            .Select(p => $"[{p.GeoPosition!.Value.Longitude},{p.GeoPosition.Value.Latitude},{p.AltitudeMSL}]")
            .ToList();

        return $$"""
        {
            "type": "Feature",
            "properties": {
                "droneId": "{{droneId}}",
                "startTime": "{{packets.FirstOrDefault()?.Timestamp:O}}",
                "endTime": "{{packets.LastOrDefault()?.Timestamp:O}}",
                "pointCount": {{packets.Count}}
            },
            "geometry": {
                "type": "LineString",
                "coordinates": [{{string.Join(",", coordinates)}}]
            }
        }
        """;
    }

    /// <summary>
    /// Export flight path to KML.
    /// </summary>
    public string ExportToKml(string droneId, IReadOnlyList<TelemetryPacket> packets)
    {
        var coordinates = string.Join("\n", packets
            .Where(p => p.GeoPosition != null)
            .Select(p => $"{p.GeoPosition!.Value.Longitude},{p.GeoPosition.Value.Latitude},{p.AltitudeMSL}"));

        return $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <kml xmlns="http://www.opengis.net/kml/2.2">
            <Document>
                <name>{droneId} Flight Path</name>
                <Placemark>
                    <name>Flight Track</name>
                    <LineString>
                        <altitudeMode>absolute</altitudeMode>
                        <coordinates>
        {coordinates}
                        </coordinates>
                    </LineString>
                </Placemark>
            </Document>
        </kml>
        """;
    }
}
