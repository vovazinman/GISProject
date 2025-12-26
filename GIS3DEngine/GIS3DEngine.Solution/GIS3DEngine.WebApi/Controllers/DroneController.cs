using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.WebApi.Dtos;
using GIS3DEngine.WebApi.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GIS3DEngine.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DroneController : ControllerBase
{
    private readonly DroneFleetManager _fleet;
    private readonly IHubContext<DroneHub> _hubContext;
    private readonly ILogger<DroneController> _logger;

    public DroneController(
        DroneFleetManager fleet,
        IHubContext<DroneHub> hubContext,
        ILogger<DroneController> logger)
    {
        _fleet = fleet;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all drones
    /// GET /api/drone
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<DroneStateDto>> GetAll()
    {
        var drones = _fleet.GetAllDrones();
        return Ok(drones.Select(DroneStateDto.From));
    }

    /// <summary>
    /// Get specific drone
    /// GET /api/drone/{id}
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<DroneStateDto> Get(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        return Ok(DroneStateDto.From(drone));
    }

    /// <summary>
    /// Create new drone
    /// POST /api/drone
    /// </summary>
    [HttpPost]
    public ActionResult<DroneStateDto> Create([FromBody] CreateDroneRequest request)
    {
        var specs = request.SpecsType?.ToLower() switch
        {
            "mavic3" => DroneSpecifications.DJIMavic3,
           // "phantom4" => DroneSpecifications.DJIPhantom4,
            "matrice300" => DroneSpecifications.DJIMatrice300,
            _ => DroneSpecifications.DJIMavic3
        };

        var drone = new Drone(request.Id ?? Guid.NewGuid().ToString(), specs);
        drone.Initialize(new Vector3D(request.X, request.Y, request.Z));

        _fleet.AddDrone(drone);

        // Subscribe to state changes for real-time updates
        drone.StateChanged += async (s, e) =>
        {
            await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));
        };

        _logger.LogInformation("Created drone {DroneId}", drone.Id);

        return CreatedAtAction(nameof(Get), new { id = drone.Id }, DroneStateDto.From(drone));
    }

    /// <summary>
    /// Arm drone
    /// POST /api/drone/{id}/arm
    /// </summary>
    [HttpPost("{id}/arm")]
    public async Task<ActionResult<CommandResponse>> Arm(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        var success = drone.Arm();

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? "Drone armed" : "Failed to arm drone",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Disarm drone
    /// POST /api/drone/{id}/disarm
    /// </summary>
    [HttpPost("{id}/disarm")]
    public async Task<ActionResult<CommandResponse>> Disarm(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        var success = drone.Disarm();

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? "Drone disarmed" : "Failed to disarm drone",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Takeoff
    /// POST /api/drone/{id}/takeoff?altitude=30
    /// </summary>
    [HttpPost("{id}/takeoff")]
    public async Task<ActionResult<CommandResponse>> Takeoff(string id, [FromQuery] double altitude = 30)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        if (!drone.State.IsArmed)
            drone.Arm();

        var success = drone.Takeoff(altitude);

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? $"Taking off to {altitude}m" : "Failed to takeoff",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Land
    /// POST /api/drone/{id}/land
    /// </summary>
    [HttpPost("{id}/land")]
    public async Task<ActionResult<CommandResponse>> Land(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        var success = drone.Land();

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? "Landing" : "Failed to land",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Go to position
    /// POST /api/drone/{id}/goto
    /// </summary>
    [HttpPost("{id}/goto")]
    public async Task<ActionResult<CommandResponse>> GoTo(string id, [FromBody] GoToRequest request)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        var target = new Vector3D(request.X, request.Y, request.Z);
        var success = drone.GoTo(target, request.Speed);

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? $"Flying to ({request.X}, {request.Y}, {request.Z})" : "Failed to navigate",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Return to launch
    /// POST /api/drone/{id}/rtl
    /// </summary>
    [HttpPost("{id}/rtl")]
    public async Task<ActionResult<CommandResponse>> ReturnToLaunch(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        var success = drone.ReturnToLaunch();

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? "Returning to launch" : "Failed to RTL",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Emergency stop
    /// POST /api/drone/{id}/emergency
    /// </summary>
    [HttpPost("{id}/emergency")]
    public async Task<ActionResult<CommandResponse>> Emergency(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        drone.EmergencyStop();

        // Send alert + state update
        await _hubContext.Clients.All.SendAsync("AlertReceived", new
        {
            droneId = id,
            alertType = "emergency",
            message = "Emergency stop activated!",
            timestamp = DateTime.UtcNow
        });

        await BroadcastDroneState(drone);

        return Ok(new CommandResponse
        {
            Success = true,
            Message = "Emergency stop activated!",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Reset drone from emergency state.
    /// POST /api/drone/{id}/reset
    /// </summary>
    [HttpPost("{id}/reset")]
    public async Task<ActionResult<CommandResponse>> ResetEmergency(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        var success = drone.ResetEmergency();

        if (success)
        {
            await BroadcastDroneState(drone);
        }

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? "Emergency reset - drone ready" : "Drone is not in emergency state",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Get flight path
    /// GET /api/drone/{id}/path
    /// </summary>
    [HttpGet("{id}/path")]
    public ActionResult<FlightPathDto> GetFlightPath(string id)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        if (drone.CurrentPath == null)
            return Ok(new FlightPathDto { DroneId = id });

        return Ok(FlightPathDto.From(drone.Id, drone.CurrentPath));
    }

    /// <summary>
    /// Simulate drone update (for testing)
    /// POST /api/drone/{id}/simulate?deltaTime=0.1
    /// </summary>
    [HttpPost("{id}/simulate")]
    public async Task<ActionResult<DroneStateDto>> Simulate(string id, [FromQuery] double deltaTime = 0.1)
    {
        var drone = _fleet.GetDrone(id);
        if (drone == null)
            return NotFound(new ErrorResponse { Error = "Drone not found", StatusCode = 404 });

        drone.Update(deltaTime);

        await BroadcastDroneState(drone);

        return Ok(DroneStateDto.From(drone));
    }

    /// <summary>
    /// Helper: Broadcast drone state to all clients
    /// </summary>
    private async Task BroadcastDroneState(Drone drone)
    {
        await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));
    }
}