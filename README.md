<div align="center">

# 🌍 GIS 3D Geometry & Flight Visualization Engine

### A Professional-Grade Geospatial 3D Visualization Library for .NET

[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# Version](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![React](https://img.shields.io/badge/React-18.x-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=for-the-badge)]()
[![Tests](https://img.shields.io/badge/Tests-60+-blue?style=for-the-badge)]()

<p align="center">
  <strong>Transform geographic coordinates into stunning 3D visualizations</strong>
</p>

[Features](#-features) •
[Installation](#-installation) •
[Quick Start](#-quick-start) •
[Frontend](#-frontend-react) •
[API](#-webapi) •
[Testing](#-testing)

---

</div>

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
- [Project Structure](#-project-structure)
- [Quick Start](#-quick-start)
- [Frontend (React)](#-frontend-react)
- [WebAPI](#-webapi)
- [Full Stack Development](#-full-stack-development)
- [Documentation](#-documentation)
- [API Reference](#-api-reference)
- [Testing](#-testing)
- [Export Formats](#-export-formats)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🎯 Overview

**GIS3DEngine** is a comprehensive, production-ready .NET library designed for creating, manipulating, and visualizing 3D geometric structures from geographic data. It includes a full-featured **React TypeScript frontend** with real-time drone control, map visualization, and AI-powered natural language commands.

### Why GIS3DEngine?

| Challenge | Our Solution |
|-----------|--------------|
| Complex coordinate transformations | Built-in WGS84 ↔ ECEF ↔ ENU converters |
| 2D to 3D geometry conversion | One-line polygon extrusion with full customization |
| Flight path animation | Time-based, frame-independent animation system |
| Real-time visualization | React + Leaflet map with live drone tracking |
| Natural language control | AI-powered command interpreter (Claude API) |

---

## ✨ Features

### 🔷 Geometry Engine
- **Polygon Creation** - From vertices, GPS coordinates, or parametric generation
- **Validation System** - Convexity detection, self-intersection checks, winding order
- **3D Extrusion** - Transform 2D shapes into prisms, frustums, and complex solids
- **Pyramid Generation** - Regular, irregular, and truncated pyramids
- **Triangulation** - Ear-clipping algorithm for rendering-ready meshes

### ✈️ Drone Simulation
- **Real-time Control** - Arm, takeoff, land, goto, RTL, emergency
- **Mission Templates** - Orbit, survey, patrol, figure-8, spiral, search patterns
- **Flight Paths** - Linear and spline-based trajectory planning
- **AI Commands** - Natural language control in Hebrew/English

### 🖥️ Frontend Application
- **React 18** with TypeScript
- **Real-time Map** - Leaflet with drone position tracking
- **SignalR** - WebSocket for live updates
- **AI Chat** - Natural language drone control
- **Mission Panel** - Pre-built mission templates

### 🌍 Spatial Services
- **Coordinate Systems** - WGS84, ECEF, ENU with seamless conversion
- **Distance Calculations** - Haversine (fast) and Vincenty (accurate) formulas
- **Spatial Queries** - Point-in-polygon, polygon intersection, nearest point

---

## 💻 System Requirements

| Requirement | Minimum | Recommended |
|-------------|---------|-------------|
| .NET SDK | 9.0 | 9.0+ |
| Node.js | 18.x | 20.x+ |
| npm | 9.x | 10.x+ |
| OS | Windows 10, macOS 12, Ubuntu 20.04 | Latest versions |
| IDE | Any text editor | VS Code, Visual Studio 2022, JetBrains Rider |

---

## 📦 Installation

### Clone Repository

```bash
git clone https://github.com/yourusername/GIS3DEngine.git
cd GIS3DEngine.Solution
```

### Install Backend Dependencies

```bash
dotnet restore
dotnet build
```

### Install Frontend Dependencies

```bash
cd drone-control-app
npm install
```

### Verify Installation

```bash
# Run backend tests
dotnet test

# Run frontend tests
cd drone-control-app
npm test
```

---

## 📁 Project Structure

```
GIS3DEngine.Solution/
│
├── 📄 GIS3DEngine.sln                    # Visual Studio Solution
├── 📄 README.md                          # This file
│
├── 📂 src/
│   ├── 📂 GIS3DEngine.Core/              # Core geometry library
│   │   ├── 📂 Primitives/                # Vector3D, GeoCoordinate, BoundingBox
│   │   ├── 📂 Geometry/                  # Polygon2D, Polygon3D, Pyramid
│   │   ├── 📂 Animation/                 # FlightPath, Waypoint
│   │   └── 📂 Spatial/                   # CoordinateTransformer, SpatialQuery
│   │
│   ├── 📂 GIS3DEngine.Drones/            # Drone simulation library
│   │   ├── 📂 Core/                      # Drone, DroneState, DroneSpecs
│   │   ├── 📂 Fleet/                     # DroneFleetManager
│   │   ├── 📂 Missions/                  # MissionFactory, MissionTemplates
│   │   └── 📂 AI/                        # CommandInterpreter, AnthropicClient
│   │
│   ├── 📂 GIS3DEngine.WebApi/            # ASP.NET Core Web API
│   │   ├── 📂 Controllers/               # DroneController, MissionController, ChatController
│   │   ├── 📂 Hubs/                      # DroneHub (SignalR)
│   │   ├── 📂 Dtos/                      # Data Transfer Objects
│   │   └── 📄 Program.cs                 # Application entry point
│   │
│   └── 📂 GIS3DEngine.Demo/              # Console demo application
│
├── 📂 drone-control-app/                  # React TypeScript Frontend
│   ├── 📂 src/
│   │   ├── 📂 components/                # React components
│   │   │   ├── 📂 Map/                   # DroneMap, MapControls
│   │   │   ├── 📂 Controls/              # ControlButtons, StatusPanel
│   │   │   ├── 📂 Chat/                  # ChatPanel, ChatMessage
│   │   │   ├── 📂 Mission/               # MissionPanel
│   │   │   └── 📂 Layout/                # Dashboard, Header
│   │   ├── 📂 hooks/                     # Custom React hooks
│   │   ├── 📂 services/                  # API services
│   │   ├── 📂 types/                     # TypeScript types
│   │   └── 📂 styles/                    # CSS styles
│   ├── 📄 package.json
│   ├── 📄 vite.config.ts
│   └── 📄 tsconfig.json
│
└── 📂 tests/
    └── 📂 GIS3DEngine.Tests/             # Unit tests (60+ tests)
```

---

## 🚀 Quick Start

### Option 1: Full Stack (Recommended)

Run both backend and frontend together:

```bash
# Terminal 1: Start WebAPI
cd src/GIS3DEngine.WebApi
dotnet run

# Terminal 2: Start React Frontend
cd drone-control-app
npm run dev
```

Then open: **http://localhost:5173**

### Option 2: Backend Only

```bash
cd src/GIS3DEngine.WebApi
dotnet run
```

API available at: **http://localhost:5000**

Swagger UI: **http://localhost:5000/swagger**

### Option 3: Console Demo

```bash
dotnet run --project src/GIS3DEngine.Demo
```

---

## 🖥️ Frontend (React)

### Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 18.x | UI Framework |
| TypeScript | 5.x | Type Safety |
| Vite | 5.x | Build Tool |
| Leaflet | 1.9.x | Map Visualization |
| SignalR | 8.x | Real-time Communication |
| Lucide React | - | Icons |

### Running the Frontend

```bash
cd drone-control-app

# Install dependencies
npm install

# Development server (hot reload)
npm run dev

# Production build
npm run build

# Preview production build
npm run preview
```

### Environment Configuration

Create `.env` file in `drone-control-app/`:

```env
VITE_API_BASE_URL=http://localhost:5000
VITE_SIGNALR_HUB_URL=http://localhost:5000/droneHub
VITE_APP_NAME=Drone Control Center
```

### Frontend Features

| Feature | Description |
|---------|-------------|
| 🗺️ **Live Map** | Real-time drone position on Leaflet map |
| 📊 **Status Panel** | Battery, altitude, speed, position |
| 🎮 **Controls** | Arm, Takeoff, Land, RTL, Emergency |
| 💬 **AI Chat** | Natural language commands (Hebrew/English) |
| 🎯 **Missions** | Pre-built mission templates |
| 🔄 **Real-time** | SignalR WebSocket updates |

### Key Components

```
src/components/
├── Map/
│   └── DroneMap.tsx          # Leaflet map with drone marker
├── Controls/
│   ├── ControlButtons.tsx    # Flight control buttons
│   └── StatusPanel.tsx       # Drone telemetry display
├── Chat/
│   └── ChatPanel.tsx         # AI command interface
├── Mission/
│   └── MissionPanel.tsx      # Mission template selector
└── Layout/
    └── Dashboard.tsx         # Main layout component
```

---

## 🔌 WebAPI

### API Endpoints

#### Drone Control

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/drone` | Get all drones |
| GET | `/api/drone/{id}` | Get drone by ID |
| POST | `/api/drone` | Create new drone |
| POST | `/api/drone/{id}/arm` | Arm drone |
| POST | `/api/drone/{id}/disarm` | Disarm drone |
| POST | `/api/drone/{id}/takeoff` | Takeoff to altitude |
| POST | `/api/drone/{id}/land` | Land drone |
| POST | `/api/drone/{id}/goto` | Fly to position |
| POST | `/api/drone/{id}/rtl` | Return to launch |
| POST | `/api/drone/{id}/emergency` | Emergency stop |
| POST | `/api/drone/{id}/reset` | Reset from emergency |

#### Missions

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/mission/templates` | Get all mission templates |
| POST | `/api/mission/preview` | Preview mission path |
| POST | `/api/mission/execute` | Execute mission |
| POST | `/api/mission/stop/{droneId}` | Stop current mission |

#### Chat (AI)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat/{droneId}` | Send natural language command |

### SignalR Events

| Event | Direction | Description |
|-------|-----------|-------------|
| `DroneStateUpdated` | Server → Client | Drone position/status update |
| `FlightPathUpdated` | Server → Client | New flight path |
| `MissionUpdated` | Server → Client | Mission status change |
| `AlertReceived` | Server → Client | Warnings/alerts |
| `SubscribeToDrone` | Client → Server | Subscribe to drone updates |

### Example API Calls

```bash
# Get all drones
curl http://localhost:5000/api/drone

# Takeoff to 50 meters
curl -X POST http://localhost:5000/api/drone/drone-1/takeoff \
  -H "Content-Type: application/json" \
  -d '{"altitude": 50}'

# Fly to position
curl -X POST http://localhost:5000/api/drone/drone-1/goto \
  -H "Content-Type: application/json" \
  -d '{"x": 100, "y": 200, "z": 50, "speed": 15}'

# Natural language command
curl -X POST http://localhost:5000/api/chat/drone-1 \
  -H "Content-Type: application/json" \
  -d '{"message": "fly to Herzliya at 100m speed 30"}'

# Execute mission
curl -X POST http://localhost:5000/api/mission/execute \
  -H "Content-Type: application/json" \
  -d '{"droneId": "drone-1", "templateId": "orbit", "parameters": {"radius": 50, "altitude": 80}}'
```

---

## 🔧 Full Stack Development

### Development Workflow

```bash
# 1. Start backend (Terminal 1)
cd src/GIS3DEngine.WebApi
dotnet watch run   # Auto-reload on changes

# 2. Start frontend (Terminal 2)
cd drone-control-app
npm run dev        # Hot Module Replacement

# 3. Open browser
open http://localhost:5173
```

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      FRONTEND (React)                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐            │
│  │   DroneMap  │  │  Controls   │  │  ChatPanel  │            │
│  │  (Leaflet)  │  │  (Buttons)  │  │    (AI)     │            │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘            │
│         │                │                │                    │
│         └────────────────┼────────────────┘                    │
│                          │                                     │
│                    ┌─────▼─────┐                               │
│                    │  SignalR  │  (WebSocket)                  │
│                    │   Client  │                               │
│                    └─────┬─────┘                               │
└──────────────────────────┼─────────────────────────────────────┘
                           │
┌──────────────────────────┼─────────────────────────────────────┐
│                      BACKEND (ASP.NET Core)                     │
│                    ┌─────▼─────┐                               │
│                    │ DroneHub  │  (SignalR Hub)                │
│                    └─────┬─────┘                               │
│                          │                                     │
│  ┌───────────────┐  ┌────▼────┐  ┌───────────────┐           │
│  │    Drone      │◄─┤  Fleet  ├─►│   Mission     │           │
│  │  Controller   │  │ Manager │  │   Factory     │           │
│  └───────┬───────┘  └────┬────┘  └───────────────┘           │
│          │               │                                    │
│          │         ┌─────▼─────┐                              │
│          └────────►│   Drone   │◄──── FlightPath              │
│                    │ (Entity)  │                              │
│                    └───────────┘                              │
└───────────────────────────────────────────────────────────────┘
```

### CORS Configuration

The WebAPI is configured to accept requests from `http://localhost:5173`:

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

---

## 🧪 Testing

### Backend Tests (.NET)

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~Polygon2DTests"
dotnet test --filter "FullyQualifiedName~DroneTests"
dotnet test --filter "FullyQualifiedName~FlightPathTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report (requires reportgenerator)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Frontend Tests (React)

```bash
cd drone-control-app

# Run all tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage
npm run test:coverage

# Run specific test file
npm test -- --testPathPattern="DroneMap"

# Run tests with verbose output
npm test -- --verbose
```

### Test Structure

```
tests/
├── GIS3DEngine.Tests/           # Backend unit tests
│   ├── Vector3DTests.cs
│   ├── Polygon2DTests.cs
│   ├── Polygon3DTests.cs
│   ├── PyramidTests.cs
│   ├── FlightPathTests.cs
│   ├── DroneTests.cs
│   ├── MissionFactoryTests.cs
│   └── CoordinateTransformerTests.cs
│
drone-control-app/
├── src/
│   ├── __tests__/               # Frontend unit tests
│   │   ├── components/
│   │   │   ├── DroneMap.test.tsx
│   │   │   ├── ControlButtons.test.tsx
│   │   │   └── ChatPanel.test.tsx
│   │   ├── hooks/
│   │   │   └── useSignalR.test.ts
│   │   └── services/
│   │       └── api.test.ts
│   └── setupTests.ts            # Test configuration
```

### Writing Tests

#### Backend Test Example

```csharp
[Fact]
public void Drone_Takeoff_ShouldChangeStatus()
{
    // Arrange
    var drone = new Drone("test-1", DroneSpecs.Mavic3);
    drone.Arm();

    // Act
    var result = drone.Takeoff(50);

    // Assert
    Assert.True(result);
    Assert.Equal(DroneStatus.TakingOff, drone.State.Status);
}
```

#### Frontend Test Example

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { ControlButtons } from '../components/Controls/ControlButtons';

describe('ControlButtons', () => {
  it('should call onAction when Takeoff is clicked', () => {
    const mockAction = jest.fn();
    render(
      <ControlButtons 
        droneId="drone-1" 
        droneStatus="Armed" 
        onAction={mockAction} 
      />
    );

    fireEvent.click(screen.getByText('Takeoff'));
    expect(mockAction).toHaveBeenCalledWith('takeoff');
  });
});
```

### Test Categories

| Category | Tests | Description |
|----------|-------|-------------|
| **Geometry** | 20+ | Polygon, Pyramid, Vector operations |
| **Animation** | 10+ | FlightPath, interpolation, timing |
| **Drone** | 15+ | State machine, commands, simulation |
| **Missions** | 10+ | Template generation, path creation |
| **Spatial** | 8+ | Coordinate transforms, distance |
| **Frontend** | 15+ | Components, hooks, services |
| **Total** | **75+** | Full coverage |

### Continuous Integration

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet test --verbosity normal

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: drone-control-app
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '20'
      - run: npm ci
      - run: npm test -- --coverage
```

---

## 📤 Export Formats

### Wavefront OBJ
```csharp
string obj = exporter.ExportToObj(geometry, includeNormals: true);
```

### STL (Stereolithography)
```csharp
string stl = exporter.ExportToStl(geometry);
```

### GeoJSON
```csharp
string geojson = exporter.ExportToGeoJson(geometry, transformer);
```

---

## 🤝 Contributing

### Development Setup

1. Fork the repository
2. Clone your fork
3. Install dependencies (both .NET and npm)
4. Create a feature branch
5. Make your changes
6. Write tests
7. Submit a pull request

### Code Style

- **C#**: Follow Microsoft naming conventions, use XML documentation
- **TypeScript**: Use strict mode, prefer functional components
- **Git**: Conventional commits (`feat:`, `fix:`, `docs:`, etc.)

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

## 🌟 Star Us!

If you find this project useful, please consider giving it a star on GitHub!

[![GitHub stars](https://img.shields.io/github/stars/yourusername/GIS3DEngine?style=social)](https://github.com/yourusername/GIS3DEngine)

---

**Built with ❤️ for the GIS and 3D visualization community**

[Back to Top](#-gis-3d-geometry--flight-visualization-engine)

</div>
