using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.AI;
using GIS3DEngine.Drones.Core;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.Services.MissionPlanning;
using GIS3DEngine.WebApi.Dtos;
using GIS3DEngine.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ========== Services Configuration ==========

// Controllers with JSON options for proper serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// SignalR for real-time communication
builder.Services.AddSignalR();

// Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Drone Control API",
        Version = "v1",
        Description = "GIS 3D Engine - Drone Fleet Management API"
    });
});

// CORS policy for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Application services
builder.Services.AddSingleton<DroneFleetManager>();
builder.Services.AddSingleton<MissionPlanner>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["Anthropic:ApiKey"] ?? "";
    return new MissionPlanner(apiKey);
});
builder.Services.AddScoped<IMissionPlanner, AiMissionPlannerAdapter>();

// ========== Build Application ==========

var app = builder.Build();

// ========== Initialize Default Drone ==========

var fleet = app.Services.GetRequiredService<DroneFleetManager>();
fleet.CreateDrone(
    id: "drone-1",
    specs: null,
    position: new Vector3D(0, 0, 0)
);
Console.WriteLine("✅ Default drone 'drone-1' created");

// ========== Simulation Loop ==========

var hubContext = app.Services.GetRequiredService<IHubContext<DroneHub>>();
var simulationTimer = new System.Timers.Timer(100); // 10 updates per second

simulationTimer.Elapsed += async (sender, e) =>
{
    try
    {
        foreach (var drone in fleet.GetAllDrones())
        {
            // Update drone physics simulation
            drone.Update(0.1);

            // Broadcast state if drone is actively moving
            var status = drone.State.Status;
            if (status == DroneStatus.Flying ||
                status == DroneStatus.TakingOff ||
                status == DroneStatus.Landing ||
                status == DroneStatus.Returning ||
                status == DroneStatus.Hovering)
            {
                var dto = DroneStateDto.From(drone);
                await hubContext.Clients.All.SendAsync("DroneStateUpdated", dto);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Simulation] Error: {ex.Message}");
    }
};

simulationTimer.AutoReset = true;
simulationTimer.Start();
Console.WriteLine("🔄 Simulation loop started (10 updates/sec)");

// ========== Middleware Pipeline ==========

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Drone Control API v1");
        c.RoutePrefix = "swagger";
    });
}

// CORS must be before routing
app.UseCors("AllowReactApp");
app.UseAuthorization();

// ========== Map Endpoints ==========

app.MapControllers();
app.MapHub<DroneHub>("/droneHub");

// ========== Startup Information ==========

Console.WriteLine("========================================");
Console.WriteLine("🚁 Drone Control API Started!");
Console.WriteLine("========================================");
Console.WriteLine("🌐 API:        http://localhost:5000/api");
Console.WriteLine("📖 Swagger:    http://localhost:5000/swagger");
Console.WriteLine("📡 SignalR:    http://localhost:5000/droneHub");
Console.WriteLine("========================================");

app.Run();