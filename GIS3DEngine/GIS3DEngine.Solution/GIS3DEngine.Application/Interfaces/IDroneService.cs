using GIS3DEngine.Application.Dtos.Common;
using GIS3DEngine.Application.Dtos.Requests;
using GIS3DEngine.Application.Dtos.Responses;

namespace GIS3DEngine.Application.Interfaces;

public interface IDroneService
{
    // Queries
    IEnumerable<DroneStateDto> GetAllDrones();
    DroneStateDto? GetDrone(string id);
    FlightStatusDto? GetFlightStatus(string id);
    FlightPathDto? GetFlightPath(string id);

    // Commands
    Task<CommandResultDto> CreateDroneAsync(CreateDroneRequestDto request);
    Task<CommandResultDto> ArmAsync(string id);
    Task<CommandResultDto> DisarmAsync(string id);
    Task<CommandResultDto> TakeoffAsync(string id, double altitude);
    Task<CommandResultDto> LandAsync(string id);
    Task<CommandResultDto> GoToAsync(string id, GoToRequestDto request);
    Task<CommandResultDto> ReturnToLaunchAsync(string id);
    Task<CommandResultDto> EmergencyStopAsync(string id);
    Task<CommandResultDto> ResetEmergencyAsync(string id);
}