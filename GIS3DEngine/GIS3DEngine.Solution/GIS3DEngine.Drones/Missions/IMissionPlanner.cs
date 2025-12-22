using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions
{
    public interface IMissionPlanner
    {
        Task<MissionPlanResult> PlanMissionAsync(
            string description,
            DroneSpecifications specs,
            Vector3D homePosition,
            double batteryPercent);
    }
}
