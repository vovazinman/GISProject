using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions
{
    public class MissionPlanResult
    {
        public bool IsValid { get; init; }

        public MissionType? MissionType { get; init; }

        public string? ErrorMessage { get; init; }

        public double EstimatedDurationSec { get; init; }

        public double EstimatedDistanceM { get; init; }

        public double RequiredBatteryPercent { get; init; }

        public double RecommendedAltitudeM { get; init; }

        public double RecommendedSpeedMps { get; init; }

        public IReadOnlyList<string> Warnings { get; init; } = [];

        private MissionPlanResult() { }

        public static MissionPlanResult Success(
            MissionType missionType,
            double estimatedDurationSec,
            double estimatedDistanceM,
            double requiredBatteryPercent,
            double recommendedAltitudeM,
            double recommendedSpeedMps,
            IReadOnlyList<string>? warnings = null)
        {
            return new MissionPlanResult
            {
                IsValid = true,
                MissionType = missionType,
                EstimatedDurationSec = estimatedDurationSec,
                EstimatedDistanceM = estimatedDistanceM,
                RequiredBatteryPercent = requiredBatteryPercent,
                RecommendedAltitudeM = recommendedAltitudeM,
                RecommendedSpeedMps = recommendedSpeedMps,
                Warnings = warnings ?? []
            };
        }

        public static MissionPlanResult Failed(string errorMessage)
        {
            return new MissionPlanResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }


}
