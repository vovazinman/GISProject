using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Application.Dtos.Requests;
using GIS3DEngine.Application.Dtos.Responses;
using GIS3DEngine.Application.Interfaces;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.Drones.Missions;
using Microsoft.Extensions.Logging;

namespace GIS3DEngine.Application.Services;

public class MissionService : IMissionService
{
    private readonly DroneFleetManager _fleet;
    private readonly INotificationService _notifications;
    private readonly ILogger<MissionService> _logger;

    public MissionService(
        DroneFleetManager fleet,
        INotificationService notifications,
        ILogger<MissionService> logger)
    {
        _fleet = fleet;
        _notifications = notifications;
        _logger = logger;
    }

    // ========== Queries ==========

    public IEnumerable<MissionInfoDto> GetAllMissions()
    {
        return _fleet.GetAllMissions().Select(MissionInfoDto.From);
    }

    public IEnumerable<MissionInfoDto> GetMissionsForDrone(string droneId)
    {
        return _fleet.GetAllMissions()
            .Where(m => m.AssignedDroneId == droneId)
            .Select(MissionInfoDto.From);
    }

    public MissionInfoDto? GetMission(string missionId)
    {
        var mission = _fleet.GetMission(missionId);
        return mission != null ? MissionInfoDto.From(mission) : null;
    }

    public MissionInfoDto? GetActiveMission(string droneId)
    {
        var mission = _fleet.GetAllMissions()
            .FirstOrDefault(m => m.AssignedDroneId == droneId && m.Status == MissionStatus.InProgress);

        return mission != null ? MissionInfoDto.From(mission) : null;
    }

    // ========== Mission Creation ==========

    public async Task<CommandResultDto> CreateSurveyMissionAsync(SurveyMissionRequestDto request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {request.DroneId} not found");

        var mission = new SurveyMission
        {
            Name = request.Name ?? $"Survey-{DateTime.Now:HHmmss}",
            AssignedDroneId = request.DroneId,
            Altitude = request.Altitude,
            Speed = request.Speed,
            HomePosition = drone.State.Position,
            Origin = new Vector3D(request.OriginX, request.OriginY, request.Altitude),
            Width = request.Width,
            Height = request.Height,
            LineSpacing = request.LineSpacing,
            Pattern = Enum.TryParse<SurveyPattern>(request.Pattern, true, out var pattern)
                ? pattern
                : SurveyPattern.Lawnmower
        };

        // Validate
        var validation = mission.Validate();
        if (!validation.IsValid)
            return CommandResultDto.BadRequest(string.Join("; ", validation.Errors));

        // Generate path (updates EstimatedDuration/Distance)
        mission.GenerateFlightPath();

        // Register with fleet
        _fleet.RegisterMission(mission);

        _logger.LogInformation(
            "Created survey mission {MissionId} for drone {DroneId}: {Width}x{Height}m",
            mission.Id, request.DroneId, request.Width, request.Height);

        return CommandResultDto.Ok(
            $"Survey mission '{mission.Name}' created",
            MissionInfoDto.From(mission));
    }

    public async Task<CommandResultDto> CreateOrbitMissionAsync(OrbitMissionRequestDto request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {request.DroneId} not found");

        var mission = new OrbitMission
        {
            Name = request.Name ?? $"Orbit-{DateTime.Now:HHmmss}",
            AssignedDroneId = request.DroneId,
            Altitude = request.Altitude,
            Speed = request.Speed,
            HomePosition = drone.State.Position,
            OrbitCenter = new Vector3D(request.CenterX, request.CenterY, 0),
            OrbitRadius = request.Radius,
            Orbits = request.Orbits,
            Clockwise = request.Clockwise
        };

        var validation = mission.Validate();
        if (!validation.IsValid)
            return CommandResultDto.BadRequest(string.Join("; ", validation.Errors));

        mission.GenerateFlightPath();
        _fleet.RegisterMission(mission);

        _logger.LogInformation(
            "Created orbit mission {MissionId} for drone {DroneId}: R={Radius}m, {Orbits} orbits",
            mission.Id, request.DroneId, request.Radius, request.Orbits);

        return CommandResultDto.Ok(
            $"Orbit mission '{mission.Name}' created",
            MissionInfoDto.From(mission));
    }

    public async Task<CommandResultDto> CreatePatrolMissionAsync(PatrolMissionRequestDto request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {request.DroneId} not found");

        if (request.Points.Count < 2)
            return CommandResultDto.BadRequest("Patrol mission requires at least 2 points");

        var waypoints = request.Points
            .Select(p => new Vector3D(p.X, p.Y, request.Altitude))
            .ToList();

        var mission = new PatrolMission
        {
            Name = request.Name ?? $"Patrol-{DateTime.Now:HHmmss}",
            AssignedDroneId = request.DroneId,
            Altitude = request.Altitude,
            Speed = request.Speed,
            HomePosition = drone.State.Position,
            PatrolPoints = waypoints,
            Loops = request.Loops
        };

        var validation = mission.Validate();
        if (!validation.IsValid)
            return CommandResultDto.BadRequest(string.Join("; ", validation.Errors));

        mission.GenerateFlightPath();
        _fleet.RegisterMission(mission);

        _logger.LogInformation(
            "Created patrol mission {MissionId} for drone {DroneId}: {Points} points, {Loops} loops",
            mission.Id, request.DroneId, waypoints.Count, request.Loops);

        return CommandResultDto.Ok(
            $"Patrol mission '{mission.Name}' created",
            MissionInfoDto.From(mission));
    }

    public async Task<CommandResultDto> CreateDeliveryMissionAsync(DeliveryMissionRequestDto request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {request.DroneId} not found");

        var mission = new DeliveryMission
        {
            Name = request.Name ?? $"Delivery-{DateTime.Now:HHmmss}",
            AssignedDroneId = request.DroneId,
            Altitude = request.Altitude,
            Speed = request.Speed,
            HomePosition = drone.State.Position,
            PickupLocation = new Vector3D(request.PickupX, request.PickupY, request.Altitude),
            DeliveryLocation = new Vector3D(request.DeliveryX, request.DeliveryY, request.Altitude),
            HoverTimeAtPickup = request.HoverTimeAtPickup,
            HoverTimeAtDelivery = request.HoverTimeAtDelivery
        };

        var validation = mission.Validate();
        if (!validation.IsValid)
            return CommandResultDto.BadRequest(string.Join("; ", validation.Errors));

        mission.GenerateFlightPath();
        _fleet.RegisterMission(mission);

        _logger.LogInformation(
            "Created delivery mission {MissionId} for drone {DroneId}",
            mission.Id, request.DroneId);

        return CommandResultDto.Ok(
            $"Delivery mission '{mission.Name}' created",
            MissionInfoDto.From(mission));
    }

    // ========== Mission Control ==========

    public async Task<CommandResultDto> StartMissionAsync(string droneId, string missionId)
    {
        var success = _fleet.StartMission(droneId, missionId);

        if (!success)
            return CommandResultDto.BadRequest("Failed to start mission");

        var mission = _fleet.GetMission(missionId);
        var drone = _fleet.GetDrone(droneId);

        _logger.LogInformation(
            "Started mission {MissionId} for drone {DroneId}",
            missionId, droneId);

        await _notifications.BroadcastMissionUpdateAsync(droneId, missionId, "Running", 0);

        if (drone != null)
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            $"Mission '{mission?.Name}' started",
            mission != null ? MissionInfoDto.From(mission) : null);
    }

    public async Task<CommandResultDto> PauseMissionAsync(string droneId)
    {
        var success = _fleet.PauseMission(droneId);

        if (!success)
            return CommandResultDto.BadRequest("No active mission to pause");

        var drone = _fleet.GetDrone(droneId);
        var mission = GetActiveMissionInternal(droneId);

        _logger.LogInformation("Paused mission for drone {DroneId}", droneId);

        if (mission != null)
            await _notifications.BroadcastMissionUpdateAsync(droneId, mission.Id, "Paused", 0);

        if (drone != null)
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            "Mission paused",
            mission != null ? MissionInfoDto.From(mission) : null);
    }

    public async Task<CommandResultDto> ResumeMissionAsync(string droneId)
    {
        var success = _fleet.ResumeMission(droneId);

        if (!success)
            return CommandResultDto.BadRequest("No paused mission to resume");

        var drone = _fleet.GetDrone(droneId);
        var mission = GetActiveMissionInternal(droneId);

        _logger.LogInformation("Resumed mission for drone {DroneId}", droneId);

        if (mission != null)
            await _notifications.BroadcastMissionUpdateAsync(droneId, mission.Id, "Running", 0);

        if (drone != null)
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            "Mission resumed",
            mission != null ? MissionInfoDto.From(mission) : null);
    }

    public async Task<CommandResultDto> StopMissionAsync(string droneId)
    {
        var mission = GetActiveMissionInternal(droneId);

        _fleet.AbortMission(droneId);

        var drone = _fleet.GetDrone(droneId);

        _logger.LogInformation("Stopped mission for drone {DroneId}", droneId);

        if (mission != null)
            await _notifications.BroadcastMissionUpdateAsync(droneId, mission.Id, "Cancelled", 0);

        if (drone != null)
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            "Mission stopped",
            mission != null ? MissionInfoDto.From(mission) : null);
    }

    public async Task<CommandResultDto> DeleteMissionAsync(string missionId)
    {
        var mission = _fleet.GetMission(missionId);
        if (mission == null)
            return CommandResultDto.NotFound($"Mission {missionId} not found");

        if (mission.Status == MissionStatus.InProgress)
            return CommandResultDto.BadRequest("Cannot delete a running mission");

        var success = _fleet.RemoveMission(missionId);

        if (!success)
            return CommandResultDto.BadRequest("Failed to delete mission");

        _logger.LogInformation("Deleted mission {MissionId}", missionId);

        return CommandResultDto.Ok($"Mission '{mission.Name}' deleted");
    }

    // ========== AI Planning ==========

    public async Task<MissionPlanResponseDto> PlanMissionFromDescriptionAsync(MissionPlanRequestDto request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
        {
            return new MissionPlanResponseDto
            {
                Success = false,
                ErrorMessage = $"Drone {request.DroneId} not found"
            };
        }

        _logger.LogInformation(
            "AI mission planning for drone {DroneId}: {Description}",
            request.DroneId, request.Description);

        // TODO: Integrate with CommandInterpreter for AI planning
        return new MissionPlanResponseDto
        {
            Success = false,
            ErrorMessage = "AI mission planning not yet implemented"
        };
    }

    // ========== Private Helpers ==========

    private DroneMission? GetActiveMissionInternal(string droneId)
    {
        return _fleet.GetAllMissions()
            .FirstOrDefault(m => m.AssignedDroneId == droneId &&
                (m.Status == MissionStatus.InProgress || m.Status == MissionStatus.Paused));
    }
}