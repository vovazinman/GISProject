using GIS3DEngine.Drones.AI;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GIS3DEngine.WebApi.Controllers;

[ApiController]
[Route("api/mission")]
public class MissionController : ControllerBase
{
    private readonly IMissionPlanner _planner;

    public MissionController(IMissionPlanner planner)
    {
        _planner = planner;
    }

    [HttpPost("plan")]
    public async Task<IActionResult> PlanMission(MissionRequestDto request)
    {
        var plan = await _planner.PlanMissionAsync(
            request.Description,
            request.Specs,
            request.HomePosition,
            request.BatteryPercent);

        return Ok(plan);
    }
}

