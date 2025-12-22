using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.AI;


using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MissionType = GIS3DEngine.Drones.Missions.MissionType;

namespace GIS3DEngine.Services.MissionPlanning;

public class AiMissionPlannerAdapter : IMissionPlanner
{
    private readonly MissionPlanner _aiPlanner;

    public AiMissionPlannerAdapter(MissionPlanner aiPlanner)
    {
        _aiPlanner = aiPlanner;
    }

    public async Task<MissionPlanResult> PlanMissionAsync(
        string description,
        DroneSpecifications specs,
        Vector3D homePosition,
        double batteryPercent)
    {
        var plan = await _aiPlanner.PlanMissionAsync(
            description,
            specs,
            homePosition,
            batteryPercent);

        if (!plan.IsValid)
            return MissionPlanResult.Failed(plan.ErrorMessage ?? "Invalid mission");

        return MissionPlanResult.Success(
            missionType: Enum.Parse<MissionType>(plan.MissionType, true),
            estimatedDurationSec: plan.EstimatedDurationMin * 60,
            estimatedDistanceM: plan.EstimatedDistanceM,
            requiredBatteryPercent: CalculateBattery(plan, specs),
            recommendedAltitudeM: plan.RecommendedAltitude,
            recommendedSpeedMps: plan.RecommendedSpeed,
            warnings: plan.SafetyNotes);

    }

    private static double CalculateBattery(
    MissionPlan plan,
    DroneSpecifications specs)
    {
        // הערכה פשוטה: אחוז סוללה לדקה
        var batteryPerMinute =
            100.0 / specs.MaxFlightTimeMinutes;

        var estimatedMinutes = plan.EstimatedDurationMin;

        var required = estimatedMinutes * batteryPerMinute;

        // clamp ל־0–100
        return Math.Min(Math.Max(required, 0), 100);
    }
}


