using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.AI;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.WebApi.Dtos;
using GIS3DEngine.WebApi.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GIS3DEngine.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionController : ControllerBase
{
    private readonly DroneFleetManager _fleet;
    private readonly IHubContext<DroneHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<MissionController> _logger;

    public MissionController(
        DroneFleetManager fleet,
        IHubContext<DroneHub> hubContext,
        IConfiguration config,
        ILogger<MissionController> logger)
    {
        _fleet = fleet;
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    #region Query Endpoints

    /// <summary>
    /// Get all missions
    /// GET /api/mission
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<MissionInfoDto>> GetAll()
    {
        var missions = _fleet.GetAllMissions();
        return Ok(missions.Select(MissionInfoDto.From));
    }

    /// <summary>
    /// Get specific mission
    /// GET /api/mission/{id}
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<MissionInfoDto> Get(string id)
    {
        var mission = _fleet.GetMission(id);
        if (mission == null)
            return NotFound(new ErrorResponse { Error = "Mission not found", StatusCode = 404 });

        return Ok(MissionInfoDto.From(mission));
    }

    /// <summary>
    /// Get mission flight path
    /// GET /api/mission/{id}/path
    /// </summary>
    [HttpGet("{id}/path")]
    public ActionResult<FlightPathDto> GetPath(string id)
    {
        var mission = _fleet.GetMission(id);
        if (mission == null)
            return NotFound(new ErrorResponse { Error = "Mission not found", StatusCode = 404 });

        var path = mission.GenerateFlightPath();
        return Ok(FlightPathDto.From(id, path));
    }

    #endregion

    #region AI Planning

    /// <summary>
    /// Plan mission using AI (natural language)
    /// POST /api/mission/plan
    /// </summary>
    [HttpPost("plan")]
    public async Task<ActionResult<MissionPlanResponse>> PlanWithAI([FromBody] MissionPlanRequest request)
    {
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR"))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "API Key not configured",
                StatusCode = 400
            });
        }

        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Drone not found",
                StatusCode = 400
            });
        }

        try
        {
            var planner = new MissionPlanner(apiKey);

            // Plan mission from natural language description
            var plan = await planner.PlanMissionAsync(
                request.Description,
                drone.Specs,
                drone.HomePosition,
                drone.State.BatteryPercent);

            if (!plan.IsValid)
            {
                return BadRequest(new MissionPlanResponse
                {
                    Success = false,
                    ErrorMessage = plan.ErrorMessage
                });
            }

            // Generate actual mission from plan
            var mission = planner.GenerateMission(plan, drone.HomePosition);
            if (mission == null)
            {
                return BadRequest(new MissionPlanResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to generate mission from plan"
                });
            }

            // Register mission in fleet manager
            _fleet.RegisterMission(mission);

            var path = mission.GenerateFlightPath();

            _logger.LogInformation("AI planned mission {MissionId}: {Name}", mission.Id, mission.Name);

            // Broadcast new mission
            await _hubContext.Clients.All.SendAsync("MissionCreated", MissionInfoDto.From(mission));

            return Ok(new MissionPlanResponse
            {
                Success = true,
                MissionId = mission.Id,
                MissionType = mission.Type.ToString(),
                Name = mission.Name,
                WaypointCount = path.Waypoints.Count,
                EstimatedDurationMin = mission.EstimatedDurationSec / 60.0,
                EstimatedDistanceM = mission.EstimatedDistanceM,
                Waypoints = path.Waypoints.Select(WaypointDto.From).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI mission planning failed: {Description}", request.Description);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Mission planning failed",
                Details = ex.Message,
                StatusCode = 500
            });
        }
    }

    #endregion

    #region Manual Mission Creation

    /// <summary>
    /// Create survey mission
    /// POST /api/mission/survey
    /// </summary>
    [HttpPost("survey")]
    public async Task<ActionResult<MissionPlanResponse>> CreateSurvey([FromBody] SurveyMissionRequest request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        // Parse pattern
        if (!Enum.TryParse<SurveyPattern>(request.Pattern, true, out var pattern))
        {
            pattern = SurveyPattern.Lawnmower;
        }

        // Create area vertices (rectangle from origin)
        var areaVertices = new List<Vector3D>
        {
            new(request.OriginX, request.OriginY, 0),
            new(request.OriginX + request.Width, request.OriginY, 0),
            new(request.OriginX + request.Width, request.OriginY + request.Height, 0),
            new(request.OriginX, request.OriginY + request.Height, 0)
        };

        var mission = new SurveyMission
        {
            Name = request.Name ?? $"Survey {request.Width}x{request.Height}",
            AreaVertices = areaVertices,
            Altitude = request.Altitude,
            Speed = request.Speed,
            Pattern = pattern,
            LineSpacing = request.LineSpacing,
            HomePosition = drone.HomePosition
        };

        _fleet.RegisterMission(mission);
        var path = mission.GenerateFlightPath();

        _logger.LogInformation("Created survey mission {MissionId}", mission.Id);

        await _hubContext.Clients.All.SendAsync("MissionCreated", MissionInfoDto.From(mission));

        return Ok(CreateMissionResponse(mission, path));
    }

    /// <summary>
    /// Create orbit mission
    /// POST /api/mission/orbit
    /// </summary>
    [HttpPost("orbit")]
    public async Task<ActionResult<MissionPlanResponse>> CreateOrbit([FromBody] OrbitMissionRequest request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        var mission = new OrbitMission
        {
            Name = request.Name ?? $"Orbit R={request.Radius}m",
            OrbitCenter = new Vector3D(request.CenterX, request.CenterY, 0),
            OrbitRadius = request.Radius,
            Altitude = request.Altitude,
            Speed = request.Speed,
            Orbits = request.Orbits,
            Clockwise = request.Clockwise,
            HomePosition = drone.HomePosition
        };

        _fleet.RegisterMission(mission);
        var path = mission.GenerateFlightPath();

        _logger.LogInformation("Created orbit mission {MissionId}", mission.Id);

        await _hubContext.Clients.All.SendAsync("MissionCreated", MissionInfoDto.From(mission));

        return Ok(CreateMissionResponse(mission, path));
    }

    /// <summary>
    /// Create patrol mission
    /// POST /api/mission/patrol
    /// </summary>
    [HttpPost("patrol")]
    public async Task<ActionResult<MissionPlanResponse>> CreatePatrol([FromBody] PatrolMissionRequest request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        var patrolPoints = request.Points.Select(p => new Vector3D(p.X, p.Y, 0)).ToList();

        if (patrolPoints.Count < 2)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Patrol requires at least 2 points",
                StatusCode = 400
            });
        }

        var mission = new PatrolMission
        {
            Name = request.Name ?? $"Patrol {patrolPoints.Count} points",
            PatrolPath = patrolPoints,  
            Altitude = request.Altitude,
            Speed = request.Speed,
            Loops = request.Loops,
            HomePosition = drone.HomePosition
        };

        _fleet.RegisterMission(mission);
        var path = mission.GenerateFlightPath();

        _logger.LogInformation("Created patrol mission {MissionId}", mission.Id);

        await _hubContext.Clients.All.SendAsync("MissionCreated", MissionInfoDto.From(mission));

        return Ok(CreateMissionResponse(mission, path));
    }

    /// <summary>
    /// Create delivery mission
    /// POST /api/mission/delivery
    /// </summary>
    [HttpPost("delivery")]
    public async Task<ActionResult<MissionPlanResponse>> CreateDelivery([FromBody] DeliveryMissionRequest request)
    {
        var drone = _fleet.GetDrone(request.DroneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        var mission = new DeliveryMission
        {
            Name = request.Name ?? "Delivery Mission",
            PickupLocation = new Vector3D(request.PickupX, request.PickupY, 0),
            DeliveryLocation = new Vector3D(request.DeliveryX, request.DeliveryY, 0),
            Altitude = request.Altitude,
            Speed = request.Speed,
            HoverTimeAtPickup = request.HoverTimeAtPickup,
            HoverTimeAtDelivery = request.HoverTimeAtDelivery,
            HomePosition = drone.HomePosition
        };

        _fleet.RegisterMission(mission);
        var path = mission.GenerateFlightPath();

        _logger.LogInformation("Created delivery mission {MissionId}", mission.Id);

        await _hubContext.Clients.All.SendAsync("MissionCreated", MissionInfoDto.From(mission));

        return Ok(CreateMissionResponse(mission, path));
    }

    #endregion

    #region Mission Execution

    /// <summary>
    /// Start mission on drone
    /// POST /api/mission/{missionId}/start?droneId=xxx
    /// </summary>
    [HttpPost("{missionId}/start")]
    public async Task<ActionResult<CommandResponse>> StartMission(string missionId, [FromQuery] string droneId)
    {
        var drone = _fleet.GetDrone(droneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        var mission = _fleet.GetMission(missionId);
        if (mission == null)
        {
            return BadRequest(new ErrorResponse { Error = "Mission not found", StatusCode = 400 });
        }

        // Generate flight path
        var path = mission.GenerateFlightPath();

        // Arm if needed
        if (!drone.State.IsArmed)
        {
            drone.Arm();
        }

        // Start mission
        var success = drone.StartMission(missionId, path);

        if (success)
        {
            _logger.LogInformation("Started mission {MissionId} on drone {DroneId}", missionId, droneId);

            // Broadcast mission started
            await _hubContext.Clients.All.SendAsync("MissionUpdated", new
            {
                missionId,
                droneId,
                status = "Started",
                progress = 0.0
            });

            // Broadcast flight path
            await _hubContext.Clients.All.SendAsync("FlightPathUpdated", FlightPathDto.From(droneId, path));

            // Broadcast drone state
            await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));
        }

        return Ok(new CommandResponse
        {
            Success = success,
            Message = success ? $"Mission {missionId} started" : "Failed to start mission",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Pause mission
    /// POST /api/mission/{missionId}/pause?droneId=xxx
    /// </summary>
    [HttpPost("{missionId}/pause")]
    public async Task<ActionResult<CommandResponse>> PauseMission(string missionId, [FromQuery] string droneId)
    {
        var drone = _fleet.GetDrone(droneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        if (drone.CurrentMissionId != missionId)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Drone is not running this mission",
                StatusCode = 400
            });
        }

        drone.PauseMission();

        _logger.LogInformation("Paused mission {MissionId} on drone {DroneId}", missionId, droneId);

        await _hubContext.Clients.All.SendAsync("MissionUpdated", new
        {
            missionId,
            droneId,
            status = "Paused",
            progress = CalculateProgress(drone)
        });

        await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));

        return Ok(new CommandResponse
        {
            Success = true,
            Message = "Mission paused",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Resume mission
    /// POST /api/mission/{missionId}/resume?droneId=xxx
    /// </summary>
    [HttpPost("{missionId}/resume")]
    public async Task<ActionResult<CommandResponse>> ResumeMission(string missionId, [FromQuery] string droneId)
    {
        var drone = _fleet.GetDrone(droneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        if (drone.CurrentMissionId != missionId)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Drone is not running this mission",
                StatusCode = 400
            });
        }

        drone.ResumeMission();

        _logger.LogInformation("Resumed mission {MissionId} on drone {DroneId}", missionId, droneId);

        await _hubContext.Clients.All.SendAsync("MissionUpdated", new
        {
            missionId,
            droneId,
            status = "Running",
            progress = CalculateProgress(drone)
        });

        await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));

        return Ok(new CommandResponse
        {
            Success = true,
            Message = "Mission resumed",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Cancel/Stop mission
    /// POST /api/mission/{missionId}/stop?droneId=xxx
    /// </summary>
    [HttpPost("{missionId}/stop")]
    public async Task<ActionResult<CommandResponse>> StopMission(string missionId, [FromQuery] string droneId)
    {
        var drone = _fleet.GetDrone(droneId);
        if (drone == null)
        {
            return BadRequest(new ErrorResponse { Error = "Drone not found", StatusCode = 400 });
        }

        drone.CancelMission();

        _logger.LogInformation("Stopped mission {MissionId} on drone {DroneId}", missionId, droneId);

        await _hubContext.Clients.All.SendAsync("MissionUpdated", new
        {
            missionId,
            droneId,
            status = "Stopped",
            progress = 0.0
        });

        await _hubContext.Clients.All.SendAsync("DroneStateUpdated", DroneStateDto.From(drone));

        return Ok(new CommandResponse
        {
            Success = true,
            Message = "Mission stopped",
            NewState = DroneStateDto.From(drone)
        });
    }

    /// <summary>
    /// Delete mission
    /// DELETE /api/mission/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<CommandResponse>> Delete(string id)
    {
        var mission = _fleet.GetMission(id);
        if (mission == null)
        {
            return NotFound(new ErrorResponse { Error = "Mission not found", StatusCode = 404 });
        }

        _fleet.RemoveMission(id);

        _logger.LogInformation("Deleted mission {MissionId}", id);

        await _hubContext.Clients.All.SendAsync("MissionDeleted", id);

        return Ok(new CommandResponse
        {
            Success = true,
            Message = $"Mission {id} deleted"
        });
    }

    #endregion

    #region Helpers

    private static MissionPlanResponse CreateMissionResponse(DroneMission mission, Core.Animation.FlightPath path)
    {
        return new MissionPlanResponse
        {
            Success = true,
            MissionId = mission.Id,
            MissionType = mission.Type.ToString(),
            Name = mission.Name,
            WaypointCount = path.Waypoints.Count,
            EstimatedDurationMin = mission.EstimatedDurationSec / 60.0,
            EstimatedDistanceM = mission.EstimatedDistanceM,
            Waypoints = path.Waypoints.Select(WaypointDto.From).ToList()
        };
    }

    private static double CalculateProgress(Drone drone)
    {
        if (drone.State.TotalWaypoints == 0) return 0;
        return (double)drone.State.CurrentWaypointIndex / drone.State.TotalWaypoints * 100;
    }

    #endregion
}