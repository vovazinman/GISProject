# GIS3DEngine - Sequence Diagram (Layered Architecture)

## Project Layers Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                           │
│                      (GIS3DEngine.WebApi)                           │
│         Controllers, Hubs, SignalR, HTTP/REST Endpoints             │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Application Layer                            │
│                    (GIS3DEngine.Application)                        │
│      Services, DTOs, Interfaces, Business Logic Orchestration       │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         Domain Layer                                │
│              (GIS3DEngine.Drones + GIS3DEngine.Services)            │
│     Fleet Management, Missions, AI, Telemetry, Domain Logic         │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          Core Layer                                 │
│                       (GIS3DEngine.Core)                            │
│       Primitives, Geometry, Flights, Spatial, Interfaces            │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Sequence Diagram - Drone Operations

```mermaid
sequenceDiagram
    autonumber
    
    actor Client
    participant WebApi as 🌐 WebApi Layer<br/>(Controllers/Hubs)
    participant App as 📦 Application Layer<br/>(Services/DTOs)
    participant Domain as 🚁 Domain Layer<br/>(Drones/Fleet)
    participant Core as ⚙️ Core Layer<br/>(Primitives/Geometry)
    
    Note over Client, Core: === Create Drone Flow ===
    
    Client->>WebApi: POST /api/drone<br/>{id, specsType, x, y, z}
    WebApi->>WebApi: DroneController.Create()
    WebApi->>Domain: DroneFleetManager.AddDrone()
    Domain->>Domain: new Drone(id, specs)
    Domain->>Core: new Vector3D(x, y, z)
    Core-->>Domain: Position
    Domain->>Domain: drone.Initialize(position)
    Domain-->>WebApi: Drone
    WebApi->>WebApi: DroneStateDto.From(drone)
    WebApi-->>Client: 201 Created<br/>DroneStateDto
    
    Note over Client, Core: === Get All Drones Flow ===
    
    Client->>WebApi: GET /api/drone
    WebApi->>App: DroneService.GetAllDrones()
    App->>Domain: DroneFleetManager.GetAllDrones()
    Domain-->>App: IEnumerable<Drone>
    App->>App: DroneStateDto.From(drone)
    App-->>WebApi: IEnumerable<DroneStateDto>
    WebApi-->>Client: 200 OK<br/>DroneStateDto[]
    
    Note over Client, Core: === Takeoff Flow ===
    
    Client->>WebApi: POST /api/drone/{id}/takeoff<br/>{altitude}
    WebApi->>App: DroneService.TakeoffAsync()
    App->>Domain: DroneFleetManager.GetDrone(id)
    Domain-->>App: Drone
    App->>Domain: drone.Takeoff(altitude)
    Domain->>Core: FlightPath calculations
    Core-->>Domain: FlightPath
    Domain-->>App: success
    App->>App: INotificationService.NotifyDroneStateChanged()
    App-->>WebApi: CommandResultDto
    WebApi-->>Client: 200 OK<br/>CommandResultDto
    
    Note over Client, Core: === GoTo Position Flow ===
    
    Client->>WebApi: POST /api/drone/{id}/goto<br/>{x, y, z, speed}
    WebApi->>App: DroneService.GoToAsync()
    App->>Domain: DroneFleetManager.GetDrone(id)
    Domain-->>App: Drone
    App->>Core: new Vector3D(x, y, z)
    Core-->>App: target position
    App->>Domain: drone.GoTo(target, speed)
    Domain->>Core: FlightPath.CreateStraightLine()
    Core-->>Domain: FlightPath
    Domain->>Domain: drone.SetPath(path)
    Domain-->>App: success
    App-->>WebApi: GotoResponseDto
    WebApi-->>Client: 200 OK<br/>GotoResponseDto
```

---

## Sequence Diagram - Mission Operations

```mermaid
sequenceDiagram
    autonumber
    
    actor Client
    participant WebApi as 🌐 WebApi Layer<br/>(MissionController)
    participant App as 📦 Application Layer<br/>(MissionService)
    participant Domain as 🚁 Domain Layer<br/>(Fleet/Missions)
    participant AI as 🤖 AI Layer<br/>(MissionPlanner)
    participant Core as ⚙️ Core Layer
    
    Note over Client, Core: === Create Survey Mission ===
    
    Client->>WebApi: POST /api/mission/survey<br/>{droneId, origin, width, height}
    WebApi->>App: MissionService.CreateSurveyMissionAsync()
    App->>Domain: DroneFleetManager.GetDrone(id)
    Domain-->>App: Drone
    App->>Domain: new SurveyMission{...}
    Domain->>Domain: mission.Validate()
    Domain-->>App: ValidationResult
    App->>Domain: mission.GenerateFlightPath()
    Domain->>Core: FlightPath calculations
    Core-->>Domain: FlightPath
    App->>Domain: DroneFleetManager.RegisterMission()
    Domain-->>App: success
    App-->>WebApi: CommandResultDto<br/>+ MissionInfoDto
    WebApi-->>Client: 201 Created
    
    Note over Client, Core: === AI Mission Planning ===
    
    Client->>WebApi: POST /api/mission/plan<br/>{droneId, prompt}
    WebApi->>AI: MissionPlanner.PlanMissionAsync()
    AI->>AI: AnthropicClient.SendMessage()
    AI->>AI: Parse AI response
    AI->>Domain: Create mission from plan
    Domain->>Domain: mission.Validate()
    Domain->>Core: GenerateFlightPath()
    Core-->>Domain: FlightPath
    AI-->>WebApi: MissionPlanResult
    WebApi->>WebApi: MissionPlanResponseDto.From()
    WebApi-->>Client: 200 OK<br/>MissionPlanResponseDto
    
    Note over Client, Core: === Start Mission ===
    
    Client->>WebApi: POST /api/mission/{id}/start
    WebApi->>App: MissionService.StartMissionAsync()
    App->>Domain: DroneFleetManager.GetMission(id)
    Domain-->>App: Mission
    App->>Domain: DroneFleetManager.GetDrone(droneId)
    Domain-->>App: Drone
    App->>Domain: drone.StartMission(mission)
    Domain->>Core: FlightPath execution
    Core-->>Domain: path updates
    Domain->>Domain: Emit StateChanged event
    Domain-->>App: success
    App->>App: NotificationService.NotifyMissionStarted()
    App-->>WebApi: CommandResultDto
    WebApi-->>Client: 200 OK
```

---

## Sequence Diagram - Real-time Updates (SignalR)

```mermaid
sequenceDiagram
    autonumber
    
    actor Client
    participant Hub as 🔌 SignalR Hub<br/>(DroneHub)
    participant Runtime as ⏱️ DroneRuntimeService<br/>(Background)
    participant Domain as 🚁 Domain Layer<br/>(Fleet/Drones)
    participant Notify as 📣 NotificationService
    
    Note over Client, Notify: === Client Connection ===
    
    Client->>Hub: Connect to /hubs/drone
    Hub->>Hub: OnConnectedAsync()
    Hub-->>Client: Connection established
    
    Note over Client, Notify: === Telemetry Updates (Background) ===
    
    loop Every 100ms (configurable)
        Runtime->>Domain: DroneFleetManager.GetAllDrones()
        Domain-->>Runtime: IEnumerable<Drone>
        Runtime->>Domain: drone.UpdateSimulation(deltaTime)
        Domain->>Domain: Update position/state
        Domain->>Domain: Raise StateChanged event
        Domain-->>Runtime: DroneState
        Runtime->>Notify: NotifyTelemetryUpdate()
        Notify->>Hub: Clients.All.SendAsync("TelemetryUpdated")
        Hub-->>Client: TelemetryDto
    end
    
    Note over Client, Notify: === State Change Events ===
    
    Domain->>Domain: drone.StateChanged event
    Domain->>Notify: OnDroneStateChanged()
    Notify->>Hub: Clients.All.SendAsync("DroneStateUpdated")
    Hub-->>Client: DroneStateDto
    
    Note over Client, Notify: === Mission Events ===
    
    Domain->>Domain: mission.StatusChanged
    Domain->>Notify: OnMissionStatusChanged()
    Notify->>Hub: Clients.All.SendAsync("MissionStatusChanged")
    Hub-->>Client: MissionInfoDto
```

---

## Sequence Diagram - Chat/AI Assistant

```mermaid
sequenceDiagram
    autonumber
    
    actor Client
    participant WebApi as 🌐 ChatController
    participant AI as 🤖 DroneAssistant
    participant Anthropic as ☁️ Anthropic API
    participant Domain as 🚁 Domain Layer
    
    Client->>WebApi: POST /api/chat<br/>{message, droneId}
    WebApi->>Domain: Get drone context
    Domain-->>WebApi: Drone state/capabilities
    WebApi->>AI: DroneAssistant.ProcessAsync()
    AI->>AI: Build context prompt
    AI->>Anthropic: HTTP POST /messages
    Anthropic-->>AI: AI Response
    AI->>AI: CommandInterpreter.Parse()
    
    alt Command detected
        AI->>Domain: Execute command
        Domain-->>AI: Result
    end
    
    AI-->>WebApi: ChatResponse
    WebApi-->>Client: ChatResponseDto
```

---

## Layer Dependencies

```
GIS3DEngine.WebApi
    ├── GIS3DEngine.Application
    ├── GIS3DEngine.Drones
    ├── GIS3DEngine.Services
    ├── GIS3DEngine.Core
    └── GIS3DEngine.ServiceDefaults

GIS3DEngine.Application
    ├── GIS3DEngine.Drones
    └── GIS3DEngine.Core

GIS3DEngine.Drones
    └── GIS3DEngine.Core

GIS3DEngine.Services
    ├── GIS3DEngine.Drones
    └── GIS3DEngine.Core

GIS3DEngine.Core
    └── (No dependencies - Base layer)
```

---

## Key Components per Layer

| Layer | Project | Key Components |
|-------|---------|----------------|
| **Presentation** | WebApi | `DroneController`, `MissionController`, `ChatController`, `DroneHub` |
| **Application** | Application | `DroneService`, `MissionService`, `IDroneService`, `IMissionService`, DTOs |
| **Domain** | Drones | `DroneFleetManager`, `Drone`, `DroneMission`, `MissionPlanner`, `AnthropicClient` |
| **Domain** | Services | `AiMissionPlannerAdapter` |
| **Core** | Core | `Vector3D`, `FlightPath`, `FlyingObject`, `Polygon2D`, `Polygon3D` |

---

## 🆕 Proposed Infrastructure Layer

### Architecture with DB, Redis & ElasticSearch

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                           │
│                      (GIS3DEngine.WebApi)                           │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Application Layer                            │
│                    (GIS3DEngine.Application)                        │
│     Services use Interfaces (IMissionRepository, IDroneCache, etc)  │
└─────────────────────────────────────────────────────────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          ▼                       ▼                       ▼
┌───────────────────┐ ┌───────────────────┐ ┌───────────────────────┐
│   Domain Layer    │ │Infrastructure Layer│ │  Infrastructure Layer │
│   (Drones/Core)   │ │  (Persistence)     │ │  (Caching/Search)     │
│                   │ │                    │ │                       │
│  Business Logic   │ │  ✅ SQL Server/    │ │  ✅ Redis - Status    │
│  Mission Rules    │ │     PostgreSQL     │ │  ✅ ElasticSearch -   │
│  Flight Calcs     │ │  ✅ EF Core        │ │     Location Search   │
└───────────────────┘ └───────────────────┘ └───────────────────────┘
```

---

## 📦 New Project: GIS3DEngine.Infrastructure

### Recommended Structure:

```
GIS3DEngine.Infrastructure/
├── Persistence/
│   ├── GIS3DEngineDbContext.cs           # EF Core DbContext
│   ├── Configurations/
│   │   ├── MissionConfiguration.cs        # EF Fluent API
│   │   └── DroneConfiguration.cs
│   └── Repositories/
│       ├── MissionRepository.cs           # IMissionRepository impl
│       └── DroneRepository.cs             # IDroneRepository impl
│
├── Caching/
│   ├── RedisDroneStatusCache.cs           # IDroneStatusCache impl
│   └── RedisConnectionFactory.cs
│
├── Search/
│   ├── ElasticDroneSearchService.cs       # IDroneSearchService impl
│   └── ElasticSearchConfiguration.cs
│
└── DependencyInjection.cs                 # Extension methods
```

---

## Sequence Diagram - Mission DB Persistence

```mermaid
sequenceDiagram
    autonumber

    actor Client
    participant WebApi as 🌐 WebApi
    participant App as 📦 Application<br/>(MissionService)
    participant Domain as 🚁 Domain<br/>(Validation)
    participant Repo as 💾 Infrastructure<br/>(MissionRepository)
    participant DB as 🗄️ SQL Server/<br/>PostgreSQL

    Note over Client, DB: === Create Mission with DB Persistence ===

    Client->>WebApi: POST /api/mission/survey
    WebApi->>App: MissionService.CreateSurveyMissionAsync()

    App->>Domain: Create & Validate Mission
    Domain-->>App: SurveyMission (validated)

    App->>Domain: mission.GenerateFlightPath()
    Domain-->>App: FlightPath

    App->>Repo: IMissionRepository.AddAsync(mission)
    Repo->>DB: INSERT INTO Missions...
    DB-->>Repo: Mission ID
    Repo-->>App: MissionEntity

    App->>Domain: FleetManager.RegisterMission()
    Domain-->>App: success

    App-->>WebApi: CommandResultDto
    WebApi-->>Client: 201 Created

    Note over Client, DB: === Update Mission Status ===

    Domain->>Domain: Mission status changed
    App->>Repo: IMissionRepository.UpdateStatusAsync()
    Repo->>DB: UPDATE Missions SET Status=...
    DB-->>Repo: OK
```

---

## Sequence Diagram - Redis Drone Status

```mermaid
sequenceDiagram
    autonumber

    actor Client
    participant WebApi as 🌐 WebApi
    participant App as 📦 Application<br/>(DroneService)
    participant Cache as 🔴 Infrastructure<br/>(RedisDroneCache)
    participant Redis as 📦 Redis Server
    participant Domain as 🚁 Domain<br/>(FleetManager)

    Note over Client, Domain: === Get Drone Status (Cache First) ===

    Client->>WebApi: GET /api/drone/{id}/status
    WebApi->>App: DroneService.GetDroneStatus(id)

    App->>Cache: IDroneStatusCache.GetAsync(id)
    Cache->>Redis: GET drone:status:{id}

    alt Cache Hit ✅
        Redis-->>Cache: DroneStatusDto (JSON)
        Cache-->>App: DroneStatusDto
    else Cache Miss ❌
        Redis-->>Cache: null
        Cache-->>App: null
        App->>Domain: FleetManager.GetDrone(id)
        Domain-->>App: Drone
        App->>Cache: SetAsync(id, status, TTL=5s)
        Cache->>Redis: SETEX drone:status:{id} 5 {json}
    end

    App-->>WebApi: DroneStatusDto
    WebApi-->>Client: 200 OK

    Note over Client, Domain: === Real-time Status Updates ===

    loop Every 100ms (Background Service)
        Domain->>Domain: drone.UpdateSimulation()
        Domain->>App: StateChanged event
        App->>Cache: IDroneStatusCache.SetAsync()
        Cache->>Redis: SETEX drone:status:{id} 5 {json}
        Cache->>Redis: PUBLISH drone:updates {json}
    end
```

---

## Sequence Diagram - ElasticSearch Location Search

```mermaid
sequenceDiagram
    autonumber

    actor Client
    participant WebApi as 🌐 WebApi
    participant App as 📦 Application<br/>(DroneSearchService)
    participant Search as 🔍 Infrastructure<br/>(ElasticDroneSearch)
    participant Elastic as 📊 ElasticSearch
    participant Cache as 🔴 Redis

    Note over Client, Cache: === Search Drones by Location ===

    Client->>WebApi: GET /api/drone/search?lat=32.1&lon=34.8&radius=5km
    WebApi->>App: DroneSearchService.SearchByLocationAsync()

    App->>Search: IDroneSearchService.SearchNearbyAsync(lat, lon, radius)
    Search->>Elastic: POST /drones/_search<br/>geo_distance query

    Note right of Elastic: {<br/>  "query": {<br/>    "geo_distance": {<br/>      "distance": "5km",<br/>      "location": {<br/>        "lat": 32.1,<br/>        "lon": 34.8<br/>      }<br/>    }<br/>  }<br/>}

    Elastic-->>Search: Matching drone IDs

    loop For each drone ID
        Search->>Cache: Get latest status from Redis
        Cache-->>Search: DroneStatusDto
    end

    Search-->>App: List<DroneSearchResult>
    App-->>WebApi: DroneSearchResultDto[]
    WebApi-->>Client: 200 OK

    Note over Client, Cache: === Index Drone Location (Background) ===

    loop Every 1 second
        App->>Search: IDroneSearchService.IndexLocationAsync()
        Search->>Elastic: POST /drones/_doc/{id}<br/>{location, timestamp, status}
        Elastic-->>Search: indexed
    end
```

---

## Interfaces to Add in Application Layer

```csharp
// GIS3DEngine.Application/Interfaces/IMissionRepository.cs
public interface IMissionRepository
{
    Task<MissionEntity?> GetByIdAsync(string id);
    Task<IEnumerable<MissionEntity>> GetByDroneIdAsync(string droneId);
    Task<IEnumerable<MissionEntity>> GetAllAsync();
    Task<MissionEntity> AddAsync(MissionEntity mission);
    Task UpdateStatusAsync(string id, MissionStatus status);
    Task DeleteAsync(string id);
}

// GIS3DEngine.Application/Interfaces/IDroneStatusCache.cs
public interface IDroneStatusCache
{
    Task<DroneStatusDto?> GetAsync(string droneId);
    Task SetAsync(string droneId, DroneStatusDto status, TimeSpan? ttl = null);
    Task RemoveAsync(string droneId);
    Task<IEnumerable<DroneStatusDto>> GetAllAsync();
}

// GIS3DEngine.Application/Interfaces/IDroneSearchService.cs
public interface IDroneSearchService
{
    Task<IEnumerable<DroneSearchResult>> SearchNearbyAsync(
        double latitude, double longitude, double radiusKm);
    Task<IEnumerable<DroneSearchResult>> SearchByStatusAsync(DroneStatus status);
    Task<IEnumerable<DroneSearchResult>> SearchByMissionTypeAsync(string missionType);
    Task IndexLocationAsync(string droneId, double lat, double lon, DroneStatus status);
}
```

---

## Updated Layer Dependencies

```
GIS3DEngine.WebApi
    ├── GIS3DEngine.Application
    ├── GIS3DEngine.Infrastructure  ← 🆕 Register DI services
    └── GIS3DEngine.ServiceDefaults

GIS3DEngine.Application
    ├── GIS3DEngine.Drones
    ├── GIS3DEngine.Core
    └── (Interfaces only - no infra dependencies)

GIS3DEngine.Infrastructure  ← 🆕
    ├── GIS3DEngine.Application (for interfaces)
    ├── GIS3DEngine.Core
    ├── Microsoft.EntityFrameworkCore.SqlServer
    ├── StackExchange.Redis
    └── NEST (ElasticSearch client)

GIS3DEngine.Drones
    └── GIS3DEngine.Core

GIS3DEngine.Core
    └── (No dependencies)
```

---

## Summary: Where Each Technology Belongs

| Technology | Layer | Purpose | Interface |
|------------|-------|---------|-----------|
| **SQL Server/PostgreSQL** | Infrastructure | Mission persistence, History | `IMissionRepository` |
| **Redis** | Infrastructure | Real-time drone status cache | `IDroneStatusCache` |
| **ElasticSearch** | Infrastructure | Geo-location search | `IDroneSearchService` |

### Data Flow:
1. **Write Path**: Domain → Application → Infrastructure → External Service
2. **Read Path**: Client → WebApi → Application → Cache/Search → (fallback to Domain)
