using GIS3DEngine.Drones.AI;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.Services.MissionPlanning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<MissionPlanner>(); // AI engine
builder.Services.AddScoped<IMissionPlanner, AiMissionPlannerAdapter>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
