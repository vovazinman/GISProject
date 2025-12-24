using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Drones.AI;
using GIS3DEngine.Drones.Fleet;
using GIS3DEngine.Drones.Missions;
using GIS3DEngine.Services.MissionPlanning;
using GIS3DEngine.WebApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ========== Services ==========

// Controllers & API
builder.Services.AddControllers();
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

// SignalR
builder.Services.AddSignalR();

// CORS - Allow React App
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

// Drone Fleet Manager
builder.Services.AddSingleton<DroneFleetManager>();

// Mission Planner - with API Key from configuration
builder.Services.AddSingleton<MissionPlanner>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["Anthropic:ApiKey"] ?? "";
    return new MissionPlanner(apiKey);
});

builder.Services.AddScoped<IMissionPlanner, AiMissionPlannerAdapter>();

// ========== Build App ==========

var app = builder.Build();

// ========== Initialize Default Drone ==========

var fleet = app.Services.GetRequiredService<DroneFleetManager>();
fleet.CreateDrone(
    id: "drone-1",
    specs: null,
    position: new Vector3D(0, 0, 0)
);
Console.WriteLine("✅ Default drone 'drone-1' created");

// ========== Middleware Pipeline ==========

// Swagger - Development only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Drone Control API v1");
        c.RoutePrefix = "swagger";
    });
}

// CORS - must be before routing
app.UseCors("AllowReactApp");

app.UseAuthorization();

// ========== Endpoints ==========

app.MapControllers();
app.MapHub<DroneHub>("/droneHub");

// ========== Startup Info ==========

Console.WriteLine("========================================");
Console.WriteLine("🚁 Drone Control API Started!");
Console.WriteLine("========================================");
Console.WriteLine($"🌐 API:        http://localhost:5000/api");
Console.WriteLine($"📖 Swagger:    http://localhost:5000/swagger");
Console.WriteLine($"📡 SignalR:    http://localhost:5000/droneHub");
Console.WriteLine("========================================");

app.Run();