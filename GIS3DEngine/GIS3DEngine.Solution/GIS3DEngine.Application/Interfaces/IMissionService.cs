using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Application.Dtos.Requests;
using GIS3DEngine.Application.Dtos.Responses;

namespace GIS3DEngine.Application.Interfaces;

public interface IMissionService
{
    // ========== Queries ==========
    IEnumerable<MissionInfoDto> GetAllMissions();
    IEnumerable<MissionInfoDto> GetMissionsForDrone(string droneId);
    MissionInfoDto? GetMission(string missionId);
    MissionInfoDto? GetActiveMission(string droneId);

    // ========== Mission Creation ==========
    Task<CommandResultDto> CreateSurveyMissionAsync(SurveyMissionRequestDto request);
    Task<CommandResultDto> CreateOrbitMissionAsync(OrbitMissionRequestDto request);
    Task<CommandResultDto> CreatePatrolMissionAsync(PatrolMissionRequestDto request);
    Task<CommandResultDto> CreateDeliveryMissionAsync(DeliveryMissionRequestDto request);

    // ========== Mission Control ==========
    Task<CommandResultDto> StartMissionAsync(string droneId, string missionId);
    Task<CommandResultDto> PauseMissionAsync(string droneId);
    Task<CommandResultDto> ResumeMissionAsync(string droneId);
    Task<CommandResultDto> StopMissionAsync(string droneId);
    Task<CommandResultDto> DeleteMissionAsync(string missionId);

    // ========== AI Planning ==========
    Task<MissionPlanResponseDto> PlanMissionFromDescriptionAsync(MissionPlanRequestDto request);
}