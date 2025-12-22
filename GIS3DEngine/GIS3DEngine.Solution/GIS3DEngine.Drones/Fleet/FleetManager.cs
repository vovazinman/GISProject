using System.Collections.Concurrent;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Missions;

namespace GIS3DEngine.Drones.Fleet;

/// <summary>
/// Manages a fleet of drones with centralized control.
/// </summary>
public class DroneFleetManager
{
    private readonly ConcurrentDictionary<string, Drone> _drones = new();
    private readonly ConcurrentDictionary<string, DroneMission> _missions = new();
    private readonly ConcurrentDictionary<string, string> _droneToMission = new();
    private readonly object _lock = new();

    /// <summary>Event fired when drone is added.</summary>
    public event EventHandler<DroneAddedEventArgs>? DroneAdded;

    /// <summary>Event fired when drone is removed.</summary>
    public event EventHandler<DroneRemovedEventArgs>? DroneRemoved;

    /// <summary>Event fired when mission status changes.</summary>
    public event EventHandler<MissionStatusChangedEventArgs>? MissionStatusChanged;

    /// <summary>Event fired on fleet-wide emergency.</summary>
    public event EventHandler<FleetEmergencyEventArgs>? FleetEmergency;

    #region Properties

    /// <summary>Number of drones in fleet.</summary>
    public int DroneCount => _drones.Count;

    /// <summary>Number of active missions.</summary>
    public int ActiveMissionCount => _missions.Values.Count(m => m.Status == MissionStatus.InProgress);

    /// <summary>All drone IDs.</summary>
    public IReadOnlyList<string> DroneIds => _drones.Keys.ToList();

    /// <summary>All mission IDs.</summary>
    public IReadOnlyList<string> MissionIds => _missions.Keys.ToList();

    #endregion

    #region Drone Management

    /// <summary>
    /// Add a drone to the fleet.
    /// </summary>
    public bool AddDrone(Drone drone)
    {
        if (_drones.TryAdd(drone.Id, drone))
        {
            drone.StateChanged += OnDroneStateChanged;
            drone.Emergency += OnDroneEmergency;
            drone.MissionComplete += OnDroneMissionComplete;
            DroneAdded?.Invoke(this, new DroneAddedEventArgs(drone.Id, drone.Specs.Type));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Create and add a new drone.
    /// </summary>
    public Drone CreateDrone(string id, DroneSpecifications? specs = null, Vector3D? position = null)
    {
        var drone = new Drone(id, specs);
        if (position.HasValue)
            drone.Initialize(position.Value);
        AddDrone(drone);
        return drone;
    }

    /// <summary>
    /// Remove a drone from the fleet.
    /// </summary>
    public bool RemoveDrone(string droneId)
    {
        if (_drones.TryRemove(droneId, out var drone))
        {
            drone.StateChanged -= OnDroneStateChanged;
            drone.Emergency -= OnDroneEmergency;
            drone.MissionComplete -= OnDroneMissionComplete;
            _droneToMission.TryRemove(droneId, out _);
            DroneRemoved?.Invoke(this, new DroneRemovedEventArgs(droneId));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get a drone by ID.
    /// </summary>
    public Drone? GetDrone(string droneId) =>
        _drones.TryGetValue(droneId, out var drone) ? drone : null;

    /// <summary>
    /// Get all drones.
    /// </summary>
    public IReadOnlyList<Drone> GetAllDrones() => _drones.Values.ToList();

    /// <summary>
    /// Get drones with specific status.
    /// </summary>
    public IReadOnlyList<Drone> GetDronesByStatus(DroneStatus status) =>
        _drones.Values.Where(d => d.State.Status == status).ToList();

    /// <summary>
    /// Get available (ready) drones.
    /// </summary>
    public IReadOnlyList<Drone> GetAvailableDrones() =>
        _drones.Values.Where(d =>
            d.State.Status == DroneStatus.Ready ||
            d.State.Status == DroneStatus.Landed ||
            d.State.Status == DroneStatus.Armed).ToList();

    /// <summary>
    /// Get flying drones.
    /// </summary>
    public IReadOnlyList<Drone> GetFlyingDrones() =>
        _drones.Values.Where(d =>
            d.State.Status == DroneStatus.Flying ||
            d.State.Status == DroneStatus.Hovering ||
            d.State.Status == DroneStatus.TakingOff).ToList();

    #endregion

    #region Mission Management

    /// <summary>
    /// Register a mission.
    /// </summary>
    public bool RegisterMission(DroneMission mission)
    {
        if (_missions.TryAdd(mission.Id, mission))
        {
            mission.Status = MissionStatus.Created;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get a mission by ID.
    /// </summary>
    public DroneMission? GetMission(string missionId) =>
        _missions.TryGetValue(missionId, out var mission) ? mission : null;

    /// <summary>
    /// Get all missions.
    /// </summary>
    public IReadOnlyList<DroneMission> GetAllMissions() => _missions.Values.ToList();

    /// <summary>
    /// Validate a mission.
    /// </summary>
    public MissionValidationResult ValidateMission(string missionId)
    {
        var mission = GetMission(missionId);
        if (mission == null)
            return new MissionValidationResult { IsValid = false, Errors = new() { "Mission not found" } };

        return mission.Validate();
    }

    /// <summary>
    /// Assign a mission to a drone.
    /// </summary>
    public bool AssignMission(string droneId, string missionId)
    {
        lock (_lock)
        {
            var drone = GetDrone(droneId);
            var mission = GetMission(missionId);

            if (drone == null || mission == null)
                return false;

            if (!GetAvailableDrones().Contains(drone))
                return false;

            var validation = mission.Validate();
            if (!validation.IsValid)
                return false;

            mission.AssignedDroneId = droneId;
            mission.Status = MissionStatus.Assigned;
            _droneToMission[droneId] = missionId;

            MissionStatusChanged?.Invoke(this, new MissionStatusChangedEventArgs(
                missionId, droneId, MissionStatus.Assigned));

            return true;
        }
    }

    /// <summary>
    /// Start a mission for a drone.
    /// </summary>
    public bool StartMission(string droneId, string? missionId = null)
    {
        lock (_lock)
        {
            var drone = GetDrone(droneId);
            if (drone == null) return false;

            missionId ??= _droneToMission.GetValueOrDefault(droneId);
            if (missionId == null) return false;

            var mission = GetMission(missionId);
            if (mission == null) return false;

            try
            {
                mission.HomePosition = drone.HomePosition;
                var flightPath = mission.GenerateFlightPath();

                if (drone.StartMission(missionId, flightPath))
                {
                    mission.Status = MissionStatus.InProgress;
                    mission.StartedAt = DateTime.UtcNow;

                    MissionStatusChanged?.Invoke(this, new MissionStatusChangedEventArgs(
                        missionId, droneId, MissionStatus.InProgress));

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }

    /// <summary>
    /// Pause mission for a drone.
    /// </summary>
    public bool PauseMission(string droneId)
    {
        var drone = GetDrone(droneId);
        if (drone?.PauseMission() == true)
        {
            var missionId = _droneToMission.GetValueOrDefault(droneId);
            if (missionId != null)
            {
                var mission = GetMission(missionId);
                if (mission != null)
                    mission.Status = MissionStatus.Paused;

                MissionStatusChanged?.Invoke(this, new MissionStatusChangedEventArgs(
                    missionId, droneId, MissionStatus.Paused));
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resume mission for a drone.
    /// </summary>
    public bool ResumeMission(string droneId)
    {
        var drone = GetDrone(droneId);
        if (drone?.ResumeMission() == true)
        {
            var missionId = _droneToMission.GetValueOrDefault(droneId);
            if (missionId != null)
            {
                var mission = GetMission(missionId);
                if (mission != null)
                    mission.Status = MissionStatus.InProgress;

                MissionStatusChanged?.Invoke(this, new MissionStatusChangedEventArgs(
                    missionId, droneId, MissionStatus.InProgress));
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Abort mission for a drone.
    /// </summary>
    public void AbortMission(string droneId)
    {
        var drone = GetDrone(droneId);
        if (drone != null)
        {
            drone.AbortMission();
            var missionId = _droneToMission.GetValueOrDefault(droneId);
            if (missionId != null)
            {
                var mission = GetMission(missionId);
                if (mission != null)
                    mission.Status = MissionStatus.Aborted;

                MissionStatusChanged?.Invoke(this, new MissionStatusChangedEventArgs(
                    missionId, droneId, MissionStatus.Aborted));
            }
        }
    }

    /// <summary>
    /// Remove mission from fleet
    /// </summary>
    public bool RemoveMission(string missionId)
    {
        // Check if any drone is running this mission
        foreach (var drone in _drones.Values)
        {
            if (drone.CurrentMissionId == missionId)
            {
                return false;
            }
        }

        var removed = _missions.TryRemove(missionId, out _);

        return removed;
    }

    #endregion

    #region Fleet Commands

    /// <summary>
    /// Command all drones to return to launch.
    /// </summary>
    public void RecallAllDrones()
    {
        foreach (var drone in GetFlyingDrones())
        {
            drone.ReturnToLaunch();
        }
    }

    /// <summary>
    /// Emergency stop all drones.
    /// </summary>
    public void EmergencyStopAll()
    {
        FleetEmergency?.Invoke(this, new FleetEmergencyEventArgs("Emergency stop all triggered"));
        foreach (var drone in _drones.Values)
        {
            drone.EmergencyStop();
        }
    }

    /// <summary>
    /// Pause all missions.
    /// </summary>
    public void PauseAllMissions()
    {
        foreach (var drone in GetFlyingDrones())
        {
            PauseMission(drone.Id);
        }
    }

    /// <summary>
    /// Resume all paused missions.
    /// </summary>
    public void ResumeAllMissions()
    {
        foreach (var drone in GetDronesByStatus(DroneStatus.Hovering))
        {
            ResumeMission(drone.Id);
        }
    }

    #endregion

    #region Simulation

    /// <summary>
    /// Update all drones in the fleet.
    /// </summary>
    public void Update(double deltaTime)
    {
        foreach (var drone in _drones.Values)
        {
            drone.Update(deltaTime);
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get fleet statistics.
    /// </summary>
    public FleetStatistics GetStatistics()
    {
        var drones = _drones.Values.ToList();
        var missions = _missions.Values.ToList();

        return new FleetStatistics
        {
            TotalDrones = drones.Count,
            OnlineDrones = drones.Count(d => d.State.Status != DroneStatus.Offline),
            FlyingDrones = drones.Count(d => d.State.Status == DroneStatus.Flying),
            AvailableDrones = GetAvailableDrones().Count,
            TotalMissions = missions.Count,
            ActiveMissions = missions.Count(m => m.Status == MissionStatus.InProgress),
            CompletedMissions = missions.Count(m => m.Status == MissionStatus.Completed),
            AverageBatteryPercent = drones.Average(d => d.State.BatteryPercent),
            TotalFlightTimeHours = drones.Sum(d => d.State.FlightTimeSec) / 3600,
            TotalDistanceKm = drones.Sum(d => d.State.DistanceTraveled) / 1000
        };
    }

    #endregion

    #region Event Handlers

    private void OnDroneStateChanged(object? sender, DroneStateChangedEventArgs e)
    {
        // Forward state changes, add fleet-level handling
    }

    private void OnDroneEmergency(object? sender, DroneEmergencyEventArgs e)
    {
        FleetEmergency?.Invoke(this, new FleetEmergencyEventArgs(
            $"Drone {e.DroneId} emergency: {e.Message}"));
    }

    private void OnDroneMissionComplete(object? sender, MissionCompleteEventArgs e)
    {
        var mission = GetMission(e.MissionId);
        if (mission != null)
        {
            mission.Status = MissionStatus.Completed;
            mission.CompletedAt = DateTime.UtcNow;
        }

        MissionStatusChanged?.Invoke(this, new MissionStatusChangedEventArgs(
            e.MissionId, e.DroneId, MissionStatus.Completed));

        _droneToMission.TryRemove(e.DroneId, out _);
    }

    #endregion
}

#region Fleet Event Args

public class DroneAddedEventArgs : EventArgs
{
    public string DroneId { get; }
    public DroneType Type { get; }
    public DateTime Timestamp { get; }

    public DroneAddedEventArgs(string droneId, DroneType type)
    {
        DroneId = droneId;
        Type = type;
        Timestamp = DateTime.UtcNow;
    }
}

public class DroneRemovedEventArgs : EventArgs
{
    public string DroneId { get; }
    public DateTime Timestamp { get; }

    public DroneRemovedEventArgs(string droneId)
    {
        DroneId = droneId;
        Timestamp = DateTime.UtcNow;
    }
}

public class MissionStatusChangedEventArgs : EventArgs
{
    public string MissionId { get; }
    public string DroneId { get; }
    public MissionStatus Status { get; }
    public DateTime Timestamp { get; }

    public MissionStatusChangedEventArgs(string missionId, string droneId, MissionStatus status)
    {
        MissionId = missionId;
        DroneId = droneId;
        Status = status;
        Timestamp = DateTime.UtcNow;
    }
}

public class FleetEmergencyEventArgs : EventArgs
{
    public string Message { get; }
    public DateTime Timestamp { get; }

    public FleetEmergencyEventArgs(string message)
    {
        Message = message;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion

#region Statistics

/// <summary>
/// Fleet-wide statistics.
/// </summary>
public class FleetStatistics
{
    public int TotalDrones { get; set; }
    public int OnlineDrones { get; set; }
    public int FlyingDrones { get; set; }
    public int AvailableDrones { get; set; }
    public int TotalMissions { get; set; }
    public int ActiveMissions { get; set; }
    public int CompletedMissions { get; set; }
    public double AverageBatteryPercent { get; set; }
    public double TotalFlightTimeHours { get; set; }
    public double TotalDistanceKm { get; set; }

    public override string ToString()
    {
        return $"Fleet: {TotalDrones} drones ({FlyingDrones} flying), " +
               $"{ActiveMissions}/{TotalMissions} missions active, " +
               $"Avg battery: {AverageBatteryPercent:F1}%";
    }
}

#endregion
