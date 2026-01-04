using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Application.Dtos.Requests;
using GIS3DEngine.Application.Dtos.Responses;
using GIS3DEngine.Application.Interfaces;
using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using Microsoft.Extensions.Logging;

namespace GIS3DEngine.Application.Services;

public class DroneService : IDroneService
{
    private readonly DroneFleetManager _fleet;
    private readonly INotificationService _notifications;
    private readonly ILogger<DroneService> _logger;

    public DroneService(
        DroneFleetManager fleet,
        INotificationService notifications,
        ILogger<DroneService> logger)
    {
        _fleet = fleet;
        _notifications = notifications;
        _logger = logger;
    }

    // ========== Queries ==========

    public IEnumerable<DroneStateDto> GetAllDrones()
    {
        return _fleet.GetAllDrones().Select(DroneStateDto.From);
    }

    public DroneStateDto? GetDrone(string id)
    {
        var drone = _fleet.GetDrone(id);
        return drone != null ? DroneStateDto.From(drone) : null;
    }

    public FlightStatusDto? GetFlightStatus(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null) return null;

        var destination = drone.CurrentPath?.GetFinalPosition();
        var distanceRemaining = destination.HasValue
            ? Vector3D.Distance(drone.State.Position, destination.Value)
            : 0;

        return new FlightStatusDto
        {
            DroneId = id,
            Status = drone.State.Status.ToString(),
            Position = Vector3Dto.From(drone.State.Position),
            Destination = destination.HasValue ? Vector3Dto.From(destination.Value) : null,
            DistanceRemaining = distanceRemaining,
            Progress = drone.CurrentPath?.GetProgress(drone.MissionTime) ?? 0,
            IsFlying = drone.State.Status == DroneStatus.Flying
        };
    }

    public FlightPathDto? GetFlightPath(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null || drone.CurrentPath == null)
            return null;

        return FlightPathDto.From(id, drone.CurrentPath);
    }

    // ========== Commands ==========

    public async Task<CommandResultDto> CreateDroneAsync(CreateDroneRequestDto request)
    {
        var specs = request.SpecsType?.ToLower() switch
        {
            "mavic3" => DroneSpecifications.DJIMavic3,
            "matrice300" => DroneSpecifications.DJIMatrice300,
            _ => DroneSpecifications.DJIMavic3
        };

        var droneId = request.Id ?? Guid.NewGuid().ToString();
        var drone = new Drone(droneId, specs);
        drone.Initialize(new Vector3D(request.X, request.Y, request.Z));

        _fleet.AddDrone(drone);

        _logger.LogInformation("Created drone {DroneId} at ({X}, {Y}, {Z})",
            droneId, request.X, request.Y, request.Z);

        await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            $"Drone {droneId} created",
            DroneStateDto.From(drone));
    }

    public async Task<CommandResultDto> ArmAsync(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        var success = drone.Arm();

        if (success)
        {
            _logger.LogInformation("Drone {DroneId} armed", id);
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));
        }

        return new CommandResultDto
        {
            Success = success,
            Message = success ? "Drone armed" : "Failed to arm drone",
            Data = DroneStateDto.From(drone)
        };
    }

    public async Task<CommandResultDto> DisarmAsync(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        var success = drone.Disarm();

        if (success)
        {
            _logger.LogInformation("Drone {DroneId} disarmed", id);
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));
        }

        return new CommandResultDto
        {
            Success = success,
            Message = success ? "Drone disarmed" : "Failed to disarm drone",
            Data = DroneStateDto.From(drone)
        };
    }

    public async Task<CommandResultDto> TakeoffAsync(string id, double altitude)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        // Validation
        if (altitude < 1 || altitude > 500)
            return CommandResultDto.BadRequest("Altitude must be between 1 and 500 meters");

        // Auto-arm if needed
        if (!drone.State.IsArmed)
            drone.Arm();

        var success = drone.Takeoff(altitude);

        if (success)
        {
            _logger.LogInformation("Drone {DroneId} taking off to {Altitude}m", id, altitude);
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));
        }

        return new CommandResultDto
        {
            Success = success,
            Message = success ? $"Taking off to {altitude}m" : "Failed to takeoff",
            Data = DroneStateDto.From(drone)
        };
    }

    public async Task<CommandResultDto> LandAsync(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        var success = drone.Land();

        if (success)
        {
            _logger.LogInformation("Drone {DroneId} landing", id);
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));
        }

        return new CommandResultDto
        {
            Success = success,
            Message = success ? "Landing" : "Failed to land",
            Data = DroneStateDto.From(drone)
        };
    }

    public async Task<CommandResultDto> GoToAsync(string id, GoToRequestDto request)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        // Validation
        if (request.Z < 0 || request.Z > 500)
            return CommandResultDto.BadRequest("Altitude must be between 0 and 500 meters");

        if (request.Speed < 1 || request.Speed > 100)
            return CommandResultDto.BadRequest("Speed must be between 1 and 100 m/s");

        var target = new Vector3D(request.X, request.Y, request.Z);
        var currentPos = drone.State.Position;

        // Calculate distance and ETA
        var distance = Vector3D.Distance(currentPos, target);
        var eta = distance / request.Speed;

        // Create flight path based on mode
        var path = request.Mode switch
        {
            "safe" => FlightPath.CreateSafe(currentPos, target, request.Speed),
            _ => FlightPath.CreateDirect(currentPos, target, request.Speed)
        };

        var success = drone.GoTo(target, request.Speed, path); 

        if (!success)
            return CommandResultDto.BadRequest("Cannot execute goto in current state");

        _logger.LogInformation(
            "Drone {DroneId} flying to ({X}, {Y}, {Z}) Mode={Mode} Distance={Dist:F0}m ETA={ETA:F0}s",
            id, request.X, request.Y, request.Z, request.Mode, distance, eta);

        // Broadcast updates
        var pathDto = FlightPathDto.From(drone.Id, path);
        await _notifications.BroadcastFlightPathAsync(pathDto, distance, eta);

        await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            $"Flying to destination. ETA: {eta:F0}s",
            new GotoResponseDto
            {
                Success = true,
                DroneId = id,
                Destination = Vector3Dto.From(target),
                Distance = distance,
                ETA = eta,
                Mode = request.Mode.ToString()
            });
    }

    public async Task<CommandResultDto> ReturnToLaunchAsync(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        var success = drone.ReturnToLaunch();

        if (success)
        {
            _logger.LogInformation("Drone {DroneId} returning to launch", id);
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));
        }

        return new CommandResultDto
        {
            Success = success,
            Message = success ? "Returning to launch" : "Failed to RTL",
            Data = DroneStateDto.From(drone)
        };
    }

    public async Task<CommandResultDto> EmergencyStopAsync(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        drone.EmergencyStop();

        _logger.LogWarning("Drone {DroneId} EMERGENCY STOP", id);

        await _notifications.BroadcastAlertAsync(id, "emergency", "Emergency stop activated!");
        await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));

        return CommandResultDto.Ok(
            "Emergency stop activated!",
            DroneStateDto.From(drone));
    }

    public async Task<CommandResultDto> ResetEmergencyAsync(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return CommandResultDto.NotFound($"Drone {id} not found");

        var success = drone.ResetEmergency();

        if (success)
        {
            _logger.LogInformation("Drone {DroneId} emergency reset", id);
            await _notifications.BroadcastDroneStateAsync(DroneStateDto.From(drone));
        }

        return new CommandResultDto
        {
            Success = success,
            Message = success ? "Emergency reset - drone ready" : "Drone is not in emergency state",
            Data = DroneStateDto.From(drone)
        };
    }
}