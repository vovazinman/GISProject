using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Animation;

namespace GIS3DEngine.Drones.Core;

#region Drone State

/// <summary>
/// Real-time state of a drone.
/// </summary>
public class DroneState
{
    #region Position & Movement
    public string DroneId { get; set; } = string.Empty;

    /// <summary>Current position (local coordinates).</summary>
    public Vector3D Position { get; set; } = Vector3D.Zero;

    /// <summary>Current position (geographic).</summary>
    public GeoCoordinate? GeoPosition { get; set; }

    /// <summary>Velocity vector in m/s.</summary>
    public Vector3D Velocity { get; set; } = Vector3D.Zero;

    /// <summary>Acceleration vector in m/s².</summary>
    public Vector3D Acceleration { get; set; } = Vector3D.Zero;

    /// <summary>Attitude angles (roll, pitch, yaw) in radians.</summary>
    public Vector3D Attitude { get; set; } = Vector3D.Zero;

    /// <summary>Angular velocity in rad/s.</summary>
    public Vector3D AngularVelocity { get; set; } = Vector3D.Zero;

    /// <summary>Heading in radians (0 = North, clockwise).</summary>
    public double Heading { get; set; }

    /// <summary>Ground speed in m/s.</summary>
    public double GroundSpeed => Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);

    /// <summary>Vertical speed in m/s (positive = up).</summary>
    public double VerticalSpeed => Velocity.Z;

    #endregion

    #region Altitude & Distance

    /// <summary>Altitude above ground level in meters.</summary>
    public double AltitudeAGL { get; set; }

    /// <summary>Altitude above sea level in meters.</summary>
    public double AltitudeMSL { get; set; }

    /// <summary>Distance from home position in meters.</summary>
    public double DistanceFromHome { get; set; }

    /// <summary>Distance traveled in this flight (meters).</summary>
    public double DistanceTraveled { get; set; }

    #endregion

    #region Status

    /// <summary>Current operational status.</summary>
    public DroneStatus Status { get; set; } = DroneStatus.Offline;

    /// <summary>Current flight mode.</summary>
    public FlightMode FlightMode { get; set; } = FlightMode.Manual;

    /// <summary>Is the drone armed?</summary>
    public bool IsArmed { get; set; }

    /// <summary>Is the drone in failsafe mode?</summary>
    public bool IsFailsafe { get; set; }

    #endregion

    #region Battery

    /// <summary>Battery state of charge (0-100%).</summary>
    public double BatteryPercent { get; set; } = 100.0;

    /// <summary>Battery voltage.</summary>
    public double BatteryVoltage { get; set; }

    /// <summary>Estimated remaining flight time in seconds.</summary>
    public double RemainingFlightTimeSec { get; set; }

    #endregion

    #region GPS & Signal

    /// <summary>GPS satellite count.</summary>
    public int GpsSatellites { get; set; }

    /// <summary>GPS fix quality (0-5).</summary>
    public int GpsFixQuality { get; set; }

    /// <summary>Signal strength to ground station (0-100%).</summary>
    public double SignalStrength { get; set; } = 100.0;

    #endregion

    #region Mission

    /// <summary>Total flight time in this session (seconds).</summary>
    public double FlightTimeSec { get; set; }

    /// <summary>Current mission waypoint index.</summary>
    public int CurrentWaypointIndex { get; set; }

    /// <summary>Total waypoints in current mission.</summary>
    public int TotalWaypoints { get; set; }

    /// <summary>Timestamp of this state.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    #endregion

    #region Methods

    /// <summary>
    /// Create a deep copy of the current state.
    /// </summary>
    public DroneState Clone() => new()
    {
        Position = Position,
        GeoPosition = GeoPosition,
        Velocity = Velocity,
        Acceleration = Acceleration,
        Attitude = Attitude,
        AngularVelocity = AngularVelocity,
        Heading = Heading,
        AltitudeAGL = AltitudeAGL,
        AltitudeMSL = AltitudeMSL,
        DistanceFromHome = DistanceFromHome,
        DistanceTraveled = DistanceTraveled,
        Status = Status,
        FlightMode = FlightMode,
        IsArmed = IsArmed,
        IsFailsafe = IsFailsafe,
        BatteryPercent = BatteryPercent,
        BatteryVoltage = BatteryVoltage,
        RemainingFlightTimeSec = RemainingFlightTimeSec,
        GpsSatellites = GpsSatellites,
        GpsFixQuality = GpsFixQuality,
        SignalStrength = SignalStrength,
        FlightTimeSec = FlightTimeSec,
        CurrentWaypointIndex = CurrentWaypointIndex,
        TotalWaypoints = TotalWaypoints,
        Timestamp = DateTime.UtcNow
    };

    #endregion
}

#endregion

#region Drone Class

/// <summary>
/// Represents a drone with full simulation capabilities.
/// </summary>
public class Drone
{
    private readonly object _lock = new();
    private Vector3D _homePosition;
    private FlightPath? _currentPath;
    private double _missionTime;
    private bool _isSimulating;

    #region Properties

    /// <summary>Unique drone identifier.</summary>
    public string Id { get; }

    /// <summary>Display name.</summary>
    public string Name { get; set; }

    /// <summary>Drone specifications.</summary>
    public DroneSpecifications Specs { get; }

    /// <summary>Current drone state.</summary>
    public DroneState State { get; private set; }

    /// <summary>Home position (launch point).</summary>
    public Vector3D HomePosition => _homePosition;

    /// <summary>Geographic home position.</summary>
    public GeoCoordinate? GeoHomePosition { get; private set; }

    /// <summary>Current assigned mission ID.</summary>
    public string? CurrentMissionId { get; private set; }

    /// <summary>Flight path being followed.</summary>
    public FlightPath? CurrentPath => _currentPath;

    #endregion

    #region Events

    /// <summary>Event fired on state change.</summary>
    public event EventHandler<DroneStateChangedEventArgs>? StateChanged;

    /// <summary>Event fired on waypoint reached.</summary>
    public event EventHandler<WaypointReachedEventArgs>? WaypointReached;

    /// <summary>Event fired on mission complete.</summary>
    public event EventHandler<MissionCompleteEventArgs>? MissionComplete;

    /// <summary>Event fired on emergency.</summary>
    public event EventHandler<DroneEmergencyEventArgs>? Emergency;

    #endregion

    #region Constructor

    public Drone(string id, DroneSpecifications? specs = null)
    {
        Id = id;
        Name = id;
        Specs = specs ?? new DroneSpecifications();
        State = new DroneState
        {
            BatteryVoltage = Specs.BatteryVoltage,
            RemainingFlightTimeSec = Specs.MaxFlightTimeMinutes * 60
        };
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize drone at a position.
    /// </summary>
    public void Initialize(Vector3D position, GeoCoordinate? geoPosition = null)
    {
        lock (_lock)
        {
            _homePosition = position;
            GeoHomePosition = geoPosition;
            State.Position = position;
            State.GeoPosition = geoPosition;
            State.Status = DroneStatus.Ready;
            State.AltitudeAGL = 0;
            State.DistanceFromHome = 0;
            OnStateChanged("Initialized");
        }
    }

    #endregion

    #region Basic Commands

    /// <summary>
    /// Arm the drone for flight.
    /// </summary>
    public bool Arm()
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Ready && State.Status != DroneStatus.Landed)
                return false;

            if (State.BatteryPercent < 20)
            {
                OnEmergency(EmergencyType.LowBattery, "Battery too low to arm");
                return false;
            }

            State.IsArmed = true;
            State.Status = DroneStatus.Armed;
            OnStateChanged("Armed");
            return true;
        }
    }

    /// <summary>
    /// Disarm the drone.
    /// </summary>
    public bool Disarm()
    {
        lock (_lock)
        {
            if (State.Status == DroneStatus.Flying || State.Status == DroneStatus.TakingOff)
                return false;

            State.IsArmed = false;
            State.Status = DroneStatus.Ready;
            OnStateChanged("Disarmed");
            return true;
        }
    }

    /// <summary>
    /// Takeoff to specified altitude.
    /// </summary>
    public bool Takeoff(double targetAltitude)
    {
        lock (_lock)
        {
            if (!State.IsArmed || State.Status == DroneStatus.Flying)
                return false;

            targetAltitude = Math.Clamp(targetAltitude, Specs.MinSafeAltitudeM, Specs.MaxAltitudeM);
            State.Status = DroneStatus.TakingOff;
            State.FlightMode = FlightMode.Takeoff;

            var waypoints = new[]
            {
                new Waypoint(State.Position, 0),
                new Waypoint(State.Position + new Vector3D(0, 0, targetAltitude),
                    targetAltitude / Specs.MaxClimbRateMs)
            };
            _currentPath = FlightPath.CreateLinear(waypoints);
            _missionTime = 0;
            _isSimulating = true;

            OnStateChanged($"Taking off to {targetAltitude}m");
            return true;
        }
    }

    /// <summary>
    /// Land at current position.
    /// </summary>
    public bool Land()
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Flying && State.Status != DroneStatus.Hovering)
                return false;

            State.Status = DroneStatus.Landing;
            State.FlightMode = FlightMode.Land;

            var landingTime = State.AltitudeAGL / Specs.MaxDescentRateMs;
            var waypoints = new[]
            {
                new Waypoint(State.Position, 0),
                new Waypoint(new Vector3D(State.Position.X, State.Position.Y, _homePosition.Z), landingTime)
            };
            _currentPath = FlightPath.CreateLinear(waypoints);
            _missionTime = 0;

            OnStateChanged("Landing");
            return true;
        }
    }

    /// <summary>
    /// Return to launch position.
    /// </summary>
    public bool ReturnToLaunch(double returnAltitude = 50)
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Flying && State.Status != DroneStatus.Hovering)
                return false;

            State.Status = DroneStatus.Returning;
            State.FlightMode = FlightMode.ReturnToLaunch;

            returnAltitude = Math.Max(returnAltitude, State.AltitudeAGL);
            var distance = Vector3D.Distance(
                new Vector3D(State.Position.X, State.Position.Y, 0),
                new Vector3D(_homePosition.X, _homePosition.Y, 0));
            var flightTime = distance / Specs.MaxSpeedMs;

            var waypoints = new List<Waypoint>
            {
                new(State.Position, 0),
                new(new Vector3D(State.Position.X, State.Position.Y, _homePosition.Z + returnAltitude), 5),
                new(new Vector3D(_homePosition.X, _homePosition.Y, _homePosition.Z + returnAltitude), 5 + flightTime),
                new(_homePosition, 5 + flightTime + returnAltitude / Specs.MaxDescentRateMs)
            };

            _currentPath = FlightPath.CreateSpline(waypoints);
            _missionTime = 0;

            OnStateChanged("Returning to launch");
            return true;
        }
    }

    /// <summary>
    /// Fly to a specific position.
    /// </summary>
    public bool GoTo(Vector3D targetPosition, double speed = 0)
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Flying && State.Status != DroneStatus.Hovering)
                return false;

            speed = speed > 0 ? Math.Min(speed, Specs.MaxSpeedMs) : Specs.MaxSpeedMs * 0.7;
            var distance = Vector3D.Distance(State.Position, targetPosition);
            var flightTime = distance / speed;

            State.FlightMode = FlightMode.Guided;
            State.Status = DroneStatus.Flying;

            var waypoints = new[]
            {
                new Waypoint(State.Position, 0, speed),
                new Waypoint(targetPosition, flightTime, speed)
            };
            _currentPath = FlightPath.CreateLinear(waypoints);
            _missionTime = 0;
            _isSimulating = true;

            OnStateChanged($"Going to ({targetPosition.X:F1}, {targetPosition.Y:F1}, {targetPosition.Z:F1})");
            return true;
        }
    }

    #endregion

    #region Mission Commands

    /// <summary>
    /// Start a mission with the given flight path.
    /// </summary>
    public bool StartMission(string missionId, FlightPath path)
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Flying && State.Status != DroneStatus.Hovering &&
                State.Status != DroneStatus.Armed)
                return false;

            CurrentMissionId = missionId;
            _currentPath = path;
            _missionTime = 0;
            State.FlightMode = FlightMode.Auto;
            State.CurrentWaypointIndex = 0;
            State.TotalWaypoints = path.Waypoints.Count;

            if (State.Status == DroneStatus.Armed)
            {
                State.Status = DroneStatus.TakingOff;
            }
            else
            {
                State.Status = DroneStatus.Flying;
            }

            _isSimulating = true;
            OnStateChanged($"Starting mission: {missionId}");
            return true;
        }
    }

    /// <summary>
    /// Pause current mission.
    /// </summary>
    public bool PauseMission()
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Flying)
                return false;

            _isSimulating = false;
            State.Status = DroneStatus.Hovering;
            State.FlightMode = FlightMode.Loiter;
            OnStateChanged("Mission paused - hovering");
            return true;
        }
    }

    /// <summary>
    /// Resume paused mission.
    /// </summary>
    public bool ResumeMission()
    {
        lock (_lock)
        {
            if (State.Status != DroneStatus.Hovering || _currentPath == null)
                return false;

            _isSimulating = true;
            State.Status = DroneStatus.Flying;
            State.FlightMode = FlightMode.Auto;
            OnStateChanged("Mission resumed");
            return true;
        }
    }

    /// <summary>
    /// Abort current mission and hover.
    /// </summary>
    public void AbortMission()
    {
        lock (_lock)
        {
            _isSimulating = false;
            CurrentMissionId = null;
            State.Status = DroneStatus.Hovering;
            State.FlightMode = FlightMode.Loiter;
            OnStateChanged("Mission aborted");
        }
    }

    /// <summary>
    /// Emergency stop - immediate landing.
    /// </summary>
    public void EmergencyStop()
    {
        lock (_lock)
        {
            _isSimulating = false;
            CurrentMissionId = null;
            State.Status = DroneStatus.Emergency;
            State.IsFailsafe = true;
            OnEmergency(EmergencyType.UserTriggered, "Emergency stop activated");
            Land();
        }
    }

    /// <summary>
    /// Cancel current mission and hover in place
    /// </summary>
    public void CancelMission()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(CurrentMissionId))
                return;

            CurrentMissionId = null;
            _currentPath = null;
            _missionTime = 0; 

            State.CurrentWaypointIndex = 0;
            State.TotalWaypoints = 0;
            State.FlightMode = FlightMode.Loiter;
            _isSimulating = false;  // 👈 הוסף - עוצר את הסימולציה

            OnStateChanged("Mission cancelled - hovering");
        }
    }
    #endregion

    #region Simulation

    /// <summary>
    /// Update drone simulation by deltaTime seconds.
    /// </summary>
    public void Update(double deltaTime)
    {
        lock (_lock)
        {
            if (!_isSimulating || _currentPath == null)
                return;

            var previousPosition = State.Position;
            _missionTime += deltaTime;
            State.FlightTimeSec += deltaTime;

            // Check mission complete
            if (_missionTime >= _currentPath.TotalDuration)
            {
                if (_currentPath.IsLooping)
                {
                    _missionTime = 0;
                }
                else
                {
                    CompleteMission();
                    return;
                }
            }

            // Update position and velocity
            State.Position = _currentPath.GetPositionAtTime(_missionTime);
            State.Velocity = _currentPath.GetVelocityAtTime(_missionTime);
            State.Heading = _currentPath.GetHeadingAtTime(_missionTime);

            // Calculate altitude
            State.AltitudeAGL = State.Position.Z - _homePosition.Z;
            if (State.AltitudeAGL < 0) State.AltitudeAGL = 0;

            // Calculate distances
            State.DistanceTraveled += Vector3D.Distance(previousPosition, State.Position);
            State.DistanceFromHome = Vector3D.Distance(
                new Vector3D(State.Position.X, State.Position.Y, 0),
                new Vector3D(_homePosition.X, _homePosition.Y, 0));

            // Update battery (simplified linear consumption)
            var consumptionRate = 100.0 / (Specs.MaxFlightTimeMinutes * 60);
            State.BatteryPercent -= consumptionRate * deltaTime;
            State.RemainingFlightTimeSec = State.BatteryPercent * Specs.MaxFlightTimeMinutes * 60 / 100;

            // Battery warnings
            if (State.BatteryPercent <= 20 && State.Status != DroneStatus.Returning)
            {
                OnEmergency(EmergencyType.LowBattery, $"Low battery: {State.BatteryPercent:F1}%");
                if (State.BatteryPercent <= 10)
                {
                    ReturnToLaunch();
                }
            }

            // Update status after takeoff
            if (State.Status == DroneStatus.TakingOff && State.AltitudeAGL >= Specs.MinSafeAltitudeM)
            {
                State.Status = DroneStatus.Flying;
            }

            // Check waypoints
            var waypointIndex = _currentPath.GetCurrentWaypointIndex(_missionTime);
            if (waypointIndex > State.CurrentWaypointIndex)
            {
                State.CurrentWaypointIndex = waypointIndex;
                WaypointReached?.Invoke(this, new WaypointReachedEventArgs(Id, waypointIndex));
            }

            State.Timestamp = DateTime.UtcNow;
        }
    }

    private void CompleteMission()
    {
        if (State.Status == DroneStatus.Landing)
        {
            State.Status = DroneStatus.Landed;
            State.AltitudeAGL = 0;
            State.Velocity = Vector3D.Zero;
            _isSimulating = false;
            OnStateChanged("Landed");
        }
        else if (State.Status == DroneStatus.Returning)
        {
            State.Status = DroneStatus.Landed;
            State.AltitudeAGL = 0;
            State.Position = _homePosition;
            State.Velocity = Vector3D.Zero;
            _isSimulating = false;
            OnStateChanged("Returned to home and landed");
        }
        else
        {
            State.Status = DroneStatus.Hovering;
            State.FlightMode = FlightMode.Loiter;
            _isSimulating = false;

            var missionId = CurrentMissionId;
            CurrentMissionId = null;
            MissionComplete?.Invoke(this, new MissionCompleteEventArgs(Id, missionId ?? "unknown"));
            OnStateChanged("Mission complete - hovering");
        }
    }

    #endregion

    #region Event Helpers

    private void OnStateChanged(string reason)
    {
        StateChanged?.Invoke(this, new DroneStateChangedEventArgs(Id, State.Clone(), reason));
    }

    private void OnEmergency(EmergencyType type, string message)
    {
        Emergency?.Invoke(this, new DroneEmergencyEventArgs(Id, type, message, State.Position));
    }

    #endregion
}

#endregion

#region Event Args

public class DroneStateChangedEventArgs : EventArgs
{
    public string DroneId { get; }
    public DroneState State { get; }
    public string Reason { get; }
    public DateTime Timestamp { get; }

    public DroneStateChangedEventArgs(string droneId, DroneState state, string reason)
    {
        DroneId = droneId;
        State = state;
        Reason = reason;
        Timestamp = DateTime.UtcNow;
    }
}

public class WaypointReachedEventArgs : EventArgs
{
    public string DroneId { get; }
    public int WaypointIndex { get; }
    public DateTime Timestamp { get; }

    public WaypointReachedEventArgs(string droneId, int waypointIndex)
    {
        DroneId = droneId;
        WaypointIndex = waypointIndex;
        Timestamp = DateTime.UtcNow;
    }
}

public class MissionCompleteEventArgs : EventArgs
{
    public string DroneId { get; }
    public string MissionId { get; }
    public DateTime Timestamp { get; }

    public MissionCompleteEventArgs(string droneId, string missionId)
    {
        DroneId = droneId;
        MissionId = missionId;
        Timestamp = DateTime.UtcNow;
    }
}

public class DroneEmergencyEventArgs : EventArgs
{
    public string DroneId { get; }
    public EmergencyType Type { get; }
    public string Message { get; }
    public Vector3D Position { get; }
    public DateTime Timestamp { get; }

    public DroneEmergencyEventArgs(string droneId, EmergencyType type, string message, Vector3D position)
    {
        DroneId = droneId;
        Type = type;
        Message = message;
        Position = position;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion
