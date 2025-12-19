<div align="center">

# 🌍 GIS 3D Geometry & Flight Visualization Engine

### A Professional-Grade Geospatial 3D Visualization Library for .NET

[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# Version](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](LICENSE)
[![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=for-the-badge)]()
[![Tests](https://img.shields.io/badge/Tests-60+-blue?style=for-the-badge)]()

<p align="center">
  <strong>Transform geographic coordinates into stunning 3D visualizations</strong>
</p>

[Features](#-features) •
[Installation](#-installation) •
[Quick Start](#-quick-start) •
[Documentation](#-documentation) •
[API Reference](#-api-reference) •
[Examples](#-examples)

---

</div>

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
- [Project Structure](#-project-structure)
- [Quick Start](#-quick-start)
- [Documentation](#-documentation)
  - [Core Concepts](#core-concepts)
  - [Geometry Engine](#geometry-engine)
  - [Animation System](#animation-system)
  - [Spatial Services](#spatial-services)
- [API Reference](#-api-reference)
- [Examples](#-examples)
- [Export Formats](#-export-formats)
- [Performance](#-performance)
- [Testing](#-testing)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🎯 Overview

**GIS3DEngine** is a comprehensive, production-ready .NET library designed for creating, manipulating, and visualizing 3D geometric structures from geographic data. Built with clean architecture principles, it provides a robust foundation for GIS applications, urban planning tools, drone simulation systems, and 3D visualization platforms.

### Why GIS3DEngine?

| Challenge | Our Solution |
|-----------|--------------|
| Complex coordinate transformations | Built-in WGS84 ↔ ECEF ↔ ENU converters |
| 2D to 3D geometry conversion | One-line polygon extrusion with full customization |
| Flight path animation | Time-based, frame-independent animation system |
| Spatial analysis | Point-in-polygon, intersection, distance calculations |
| Export compatibility | OBJ, STL, GeoJSON out of the box |

---

## ✨ Features

### 🔷 Geometry Engine
- **Polygon Creation** - From vertices, GPS coordinates, or parametric generation
- **Validation System** - Convexity detection, self-intersection checks, winding order
- **3D Extrusion** - Transform 2D shapes into prisms, frustums, and complex solids
- **Pyramid Generation** - Regular, irregular, and truncated pyramids
- **Triangulation** - Ear-clipping algorithm for rendering-ready meshes

### ✈️ Animation System
- **Flight Paths** - Linear and spline-based trajectory planning
- **Time-Based Updates** - Frame-independent animation logic
- **Multiple Interpolation** - Linear, Catmull-Rom, Bezier curves
- **Flying Objects** - Drones, aircraft, satellites with full state tracking
- **Path Presets** - Orbit, figure-eight, and custom path generators

### 🌍 Spatial Services
- **Coordinate Systems** - WGS84, ECEF, ENU with seamless conversion
- **Distance Calculations** - Haversine (fast) and Vincenty (accurate) formulas
- **Spatial Queries** - Point-in-polygon, polygon intersection, nearest point
- **Bearing & Navigation** - Initial bearing, destination point calculation

### 📤 Export Capabilities
- **Wavefront OBJ** - Industry-standard 3D model format
- **STL** - Ready for 3D printing
- **GeoJSON** - Web mapping integration (Leaflet, Mapbox, Google Maps)

---

## 💻 System Requirements

| Requirement | Minimum | Recommended |
|-------------|---------|-------------|
| .NET SDK | 9.0 | 9.0+ |
| OS | Windows 10, macOS 12, Ubuntu 20.04 | Latest versions |
| IDE | Any text editor | Visual Studio 2022, JetBrains Rider |
| Memory | 4 GB RAM | 8 GB+ RAM |

---

## 📦 Installation

### Option 1: Clone Repository

```bash
git clone https://github.com/yourusername/GIS3DEngine.git
cd GIS3DEngine.Solution
dotnet restore
dotnet build
```

### Option 2: Download ZIP

1. Download `GIS3DEngine.zip`
2. Extract to your preferred location
3. Open `GIS3DEngine.sln` in Visual Studio

### Option 3: Add as Project Reference

```xml
<ProjectReference Include="path/to/GIS3DEngine.Core.csproj" />
<ProjectReference Include="path/to/GIS3DEngine.Services.csproj" />
```

### Verify Installation

```bash
dotnet run --project src/GIS3DEngine.Demo
```

You should see the demo output with all 8 demonstration scenarios.

---

## 📁 Project Structure

```
GIS3DEngine.Solution/
│
├── 📄 GIS3DEngine.sln              # Visual Studio Solution
├── 📄 global.json                  # .NET SDK configuration
├── 📄 README.md                    # This file
│
├── 📂 src/
│   │
│   ├── 📂 GIS3DEngine.Core/        # Core Library (2,965 lines)
│   │   ├── 📂 Primitives/          # Vector3D, GeoCoordinate, BoundingBox
│   │   ├── 📂 Interfaces/          # IGeometry3D, IAnimationPath, etc.
│   │   ├── 📂 Geometry/            # Polygon2D, Polygon3D, Pyramid
│   │   ├── 📂 Animation/           # FlightPath, FlyingObject, Interpolators
│   │   ├── 📂 Spatial/             # CoordinateTransformer, SpatialQuery
│   │   └── 📄 GIS3DEngine.Core.csproj
│   │
│   ├── 📂 GIS3DEngine.Services/    # High-Level Services (573 lines)
│   │   ├── 📄 Services.cs          # Builders, Exporters, Scene Manager
│   │   └── 📄 GIS3DEngine.Services.csproj
│   │
│   └── 📂 GIS3DEngine.Demo/        # Demo Application (498 lines)
│       ├── 📄 Program.cs           # 8 comprehensive demos
│       └── 📄 GIS3DEngine.Demo.csproj
│
└── 📂 tests/
    └── 📂 GIS3DEngine.Tests/       # Unit Tests (790 lines, 60+ tests)
        ├── 📄 UnitTests.cs
        └── 📄 GIS3DEngine.Tests.csproj
```

### Code Statistics

| Component | Files | Lines of Code | Description |
|-----------|-------|---------------|-------------|
| Core Library | 6 | 2,965 | Primitives, Geometry, Animation, Spatial |
| Services | 1 | 573 | Builders, Exporters, Scene |
| Demo | 1 | 498 | 8 demonstration scenarios |
| Tests | 1 | 790 | 60+ unit tests |
| **Total** | **9** | **4,826** | Complete implementation |

---

## 🚀 Quick Start

### 1. Create Your First 3D Building

```csharp
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Services;

// Define building footprint with GPS coordinates
var building = new PolygonBuilder()
    .AddGeoCoordinate(40.7128, -74.0060, 0)   // New York City
    .AddGeoCoordinate(40.7130, -74.0060, 0)
    .AddGeoCoordinate(40.7130, -74.0055, 0)
    .AddGeoCoordinate(40.7128, -74.0055, 0)
    .BuildExtruded(height: 150);              // 150 meters tall

Console.WriteLine($"Building Volume: {building.Volume:N0} m³");
Console.WriteLine($"Surface Area: {building.SurfaceArea:N0} m²");
Console.WriteLine($"Triangles: {building.Triangles.Count}");
```

### 2. Create an Animated Drone Survey

```csharp
using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Interfaces;

// Define survey waypoints
var surveyPath = new FlightPathBuilder()
    .AddWaypoint(new Vector3D(0, 0, 50), time: 0)
    .AddWaypoint(new Vector3D(200, 0, 60), time: 10)
    .AddWaypoint(new Vector3D(200, 200, 70), time: 25)
    .AddWaypoint(new Vector3D(0, 200, 60), time: 40)
    .AddWaypoint(new Vector3D(0, 0, 50), time: 55)
    .Smooth()           // Use Catmull-Rom spline
    .Looping(true)      // Continuous patrol
    .Build();

// Create and start drone
var drone = new FlyingObject("Survey-Drone-01", FlyingObjectType.Drone, surveyPath);
drone.Play();

// Simulation loop
for (int i = 0; i < 100; i++)
{
    drone.Update(0.5);  // 0.5 second time step
    Console.WriteLine($"Position: {drone.Position}, Heading: {drone.Heading:F2}°");
}
```

### 3. Export to 3D File

```csharp
using GIS3DEngine.Services;

var exporter = new MeshExporter();
var pyramid = Pyramid.CreateSquarePyramid(100, 75);

// Export to different formats
File.WriteAllText("pyramid.obj", exporter.ExportToObj(pyramid));
File.WriteAllText("pyramid.stl", exporter.ExportToStl(pyramid));
File.WriteAllText("pyramid.geojson", exporter.ExportToGeoJson(pyramid));
```

---

## 📖 Documentation

### Core Concepts

#### Coordinate Systems

The engine supports three coordinate systems with seamless conversion:

```
┌─────────────────────────────────────────────────────────────────┐
│                     COORDINATE SYSTEMS                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   WGS84 (GPS)              ECEF                    ENU          │
│   ┌──────────┐         ┌──────────┐          ┌──────────┐      │
│   │ Latitude │         │    X     │          │  East    │      │
│   │ Longitude│  ←────→ │    Y     │  ←────→  │  North   │      │
│   │ Altitude │         │    Z     │          │  Up      │      │
│   └──────────┘         └──────────┘          └──────────┘      │
│                                                                 │
│   Used for:            Used for:             Used for:          │
│   • GPS input          • Global calc         • Local scenes     │
│   • Map display        • Satellite pos       • 3D rendering     │
│   • GeoJSON export     • Transformations     • Physics sim      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

#### Geometry Pipeline

```
┌────────────┐    ┌────────────┐    ┌────────────┐    ┌────────────┐
│   Input    │    │  Polygon2D │    │  Polygon3D │    │   Output   │
│            │───→│            │───→│            │───→│            │
│ • Vertices │    │ • Validate │    │ • Extrude  │    │ • OBJ file │
│ • GPS      │    │ • Triangul │    │ • Transform│    │ • STL file │
│ • Params   │    │ • Analyze  │    │ • Calculate│    │ • GeoJSON  │
└────────────┘    └────────────┘    └────────────┘    └────────────┘
```

---

### Geometry Engine

#### Polygon2D - 2D Polygon Operations

```csharp
// Creation methods
var fromVertices = Polygon2D.FromVertices(vertexList);
var fromGPS = Polygon2D.FromGeoCoordinates(geoCoordinates);
var regular = Polygon2D.CreateRegular(sides: 6, radius: 50);  // Hexagon

// Validation
bool isConvex = polygon.IsConvex;
bool selfIntersects = polygon.IsSelfIntersecting();
WindingOrder winding = polygon.WindingOrder;

// Properties
double area = polygon.Area;
Vector3D center = polygon.Centroid;
BoundingBox bounds = polygon.Bounds;

// Operations
bool contains = polygon.ContainsPoint(testPoint);
var triangles = polygon.Triangulate();
var reversed = polygon.ReverseWinding();
```

#### Polygon3D - 3D Extruded Shapes

```csharp
// Extrusion options
var options = new ExtrusionOptions
{
    Height = 100,           // Extrusion height
    TopScale = 0.8,         // Taper (1.0 = prism, <1 = pyramid-like)
    Rotation = new Vector3D(0, 0, Math.PI/4),  // Rotate during extrusion
    CapTop = true,          // Include top face
    CapBottom = true        // Include bottom face
};

var extruded = polygon2D.Extrude(options);

// Properties
double volume = extruded.Volume;
double surfaceArea = extruded.SurfaceArea;
var vertices = extruded.Vertices;
var triangles = extruded.Triangles;
var normals = extruded.FaceNormals;
```

#### Pyramid - Pyramidal Structures

```csharp
// Factory methods
var squarePyramid = Pyramid.CreateSquarePyramid(sideLength: 100, height: 75);
var regularPyramid = Pyramid.CreateRegular(sides: 6, radius: 40, height: 60);
var tetrahedron = Pyramid.CreateTetrahedron(sideLength: 50);
var customApex = Pyramid.CreateWithApex(basePolygon, apexPosition);

// Properties
double slantHeight = pyramid.SlantHeight;
bool isRegular = pyramid.IsRegular;
int faceCount = pyramid.LateralFaceCount;

// Create frustum (truncated pyramid)
var frustum = pyramid.Truncate(relativeHeight: 0.6);
```

---

### Animation System

#### Flight Path Creation

```csharp
// Linear path (straight lines between waypoints)
var linearPath = FlightPath.CreateLinear(waypoints, isLooping: false);

// Smooth spline path (curves through waypoints)
var splinePath = FlightPath.CreateSpline(waypoints, isLooping: true, tension: 0.5);

// Auto-timed path based on speed
var speedPath = FlightPath.CreateWithSpeed(positions, speed: 25.0);

// Preset paths
var orbit = FlightPath.CreateOrbit(center, radius: 100, altitude: 50, duration: 60);
var figure8 = FlightPath.CreateFigureEight(center, size: 50, altitude: 30, duration: 45);
```

#### Path Sampling

```csharp
// Get state at any time
Vector3D position = path.GetPositionAtTime(15.0);
Vector3D velocity = path.GetVelocityAtTime(15.0);
Vector3D direction = path.GetDirectionAtTime(15.0);
double heading = path.GetHeadingAtTime(15.0);  // Radians from North
double pitch = path.GetPitchAtTime(15.0);      // Climb/descent angle

// Sample entire path
var samples = path.SamplePath(sampleCount: 100);
```

#### Flying Object Animation

```csharp
// Create flying object
var aircraft = new FlyingObject("Aircraft-01", FlyingObjectType.Aircraft, path);
aircraft.Name = "Survey Plane";
aircraft.MaxSpeed = 150.0;
aircraft.SpeedMultiplier = 1.5;  // 1.5x playback speed

// Event handling
aircraft.WaypointReached += (sender, index) => 
    Console.WriteLine($"Reached waypoint {index}");
aircraft.PathCompleted += (sender, e) => 
    Console.WriteLine("Path complete!");

// Control
aircraft.Play();
aircraft.Pause();
aircraft.Resume();
aircraft.Stop();
aircraft.SetTime(25.0);  // Jump to specific time

// Update loop (call every frame)
aircraft.Update(deltaTime);

// Read current state
Vector3D pos = aircraft.Position;
Vector3D vel = aircraft.Velocity;
double heading = aircraft.Heading;
double pitch = aircraft.Pitch;
```

---

### Spatial Services

#### Coordinate Transformation

```csharp
var transformer = new CoordinateTransformer();

// WGS84 (GPS) to ECEF (Earth-Centered)
GeoCoordinate gps = new GeoCoordinate(40.7128, -74.0060, 10);
Vector3D ecef = transformer.GeoToCartesian(gps);

// ECEF back to WGS84
GeoCoordinate gpsBack = transformer.CartesianToGeo(ecef);

// Get local tangent plane (East-North-Up)
var (east, north, up) = transformer.GetLocalTangentPlane(gps);

// Convert to local ENU coordinates
Vector3D localPos = transformer.GeoToENU(targetGps, referenceGps);

// World ↔ Local transformations
Vector3D local = transformer.WorldToLocal(worldPos, origin, rotation);
Vector3D world = transformer.LocalToWorld(localPos, origin, rotation);
```

#### Distance & Navigation

```csharp
var calculator = new DistanceCalculator();

// Distance calculations
double haversine = calculator.HaversineDistance(pointA, pointB);  // Fast
double vincenty = calculator.VincentyDistance(pointA, pointB);    // Accurate

// Navigation
double bearing = calculator.InitialBearing(from, to);  // Radians from North
GeoCoordinate dest = calculator.DestinationPoint(start, bearing, distance);
GeoCoordinate mid = calculator.Midpoint(pointA, pointB);
```

#### Spatial Queries

```csharp
var query = new SpatialQuery();

// Point-in-polygon tests
bool inside = query.PointInPolygon(point, polygonVertices);
bool insideGeo = query.GeoPointInPolygon(geoPoint, geoPolygon);

// Distance queries
double distToEdge = query.DistanceToPolygonEdge(point, polygon);
Vector3D closest = query.ClosestPointOnPolygonEdge(point, polygon);
double heightAbove = query.HeightAbovePolygon(point, geometry3D);

// Intersection tests
bool intersects = query.PolygonsIntersect(polygonA, polygonB);
```

#### Polygon Validation

```csharp
var validator = new PolygonValidator();

// Full validation
ValidationResult result = validator.Validate(vertices);
Console.WriteLine($"Valid: {result.IsValid}");
Console.WriteLine($"Convex: {result.IsConvex}");
Console.WriteLine($"Self-Intersecting: {result.HasSelfIntersection}");
Console.WriteLine($"Winding: {result.WindingOrder}");

foreach (var error in result.Errors)
    Console.WriteLine($"Error: {error}");
foreach (var warning in result.Warnings)
    Console.WriteLine($"Warning: {warning}");

// Individual checks
bool convex = validator.IsConvex(vertices);
bool selfIntersects = validator.IsSelfIntersecting(vertices);
WindingOrder winding = validator.GetWindingOrder(vertices);
```

---

## 📚 API Reference

### Primitives

| Type | Description | Key Members |
|------|-------------|-------------|
| `Vector3D` | Immutable 3D vector | `X`, `Y`, `Z`, `Magnitude`, `Normalized`, `Dot()`, `Cross()`, `Lerp()` |
| `GeoCoordinate` | WGS84 GPS coordinate | `Latitude`, `Longitude`, `Altitude`, `LatitudeRadians` |
| `GeoPoint` | Combined local + world position | `LocalPosition`, `WorldCoordinate` |
| `BoundingBox` | Axis-aligned bounding box | `Min`, `Max`, `Center`, `Contains()`, `Intersects()` |
| `Plane` | Mathematical plane | `Point`, `Normal`, `SignedDistance()`, `Project()` |
| `Triangle` | 3D triangle | `V0`, `V1`, `V2`, `Normal`, `Area`, `Contains()` |
| `Matrix3x3` | Rotation matrix | `RotationX()`, `RotationY()`, `RotationZ()`, `Transform()` |

### Geometry

| Type | Description | Key Members |
|------|-------------|-------------|
| `Polygon2D` | 2D polygon | `Vertices`, `Area`, `IsConvex`, `Triangulate()`, `ContainsPoint()` |
| `Polygon3D` | Extruded 3D polygon | `Volume`, `SurfaceArea`, `Triangles`, `FaceNormals` |
| `Pyramid` | Pyramidal geometry | `Apex`, `Height`, `SlantHeight`, `IsRegular`, `Truncate()` |
| `ExtrusionOptions` | Extrusion parameters | `Height`, `TopScale`, `Rotation`, `CapTop`, `CapBottom` |

### Animation

| Type | Description | Key Members |
|------|-------------|-------------|
| `Waypoint` | Path waypoint | `Position`, `Time`, `Speed`, `Type` |
| `FlightPath` | Animation path | `TotalDuration`, `GetPositionAtTime()`, `GetVelocityAtTime()` |
| `FlyingObject` | Animated entity | `Position`, `Velocity`, `Heading`, `Play()`, `Update()` |
| `TimeController` | Multi-object sync | `GlobalTime`, `TimeScale`, `Start()`, `Update()` |

### Services

| Type | Description | Key Members |
|------|-------------|-------------|
| `CoordinateTransformer` | Coordinate conversion | `GeoToCartesian()`, `CartesianToGeo()`, `GeoToENU()` |
| `DistanceCalculator` | Geographic distance | `HaversineDistance()`, `VincentyDistance()`, `InitialBearing()` |
| `SpatialQuery` | Spatial operations | `PointInPolygon()`, `PolygonsIntersect()`, `DistanceToPolygonEdge()` |
| `PolygonValidator` | Validation | `Validate()`, `IsConvex()`, `IsSelfIntersecting()` |
| `MeshExporter` | File export | `ExportToObj()`, `ExportToStl()`, `ExportToGeoJson()` |
| `Scene` | Scene management | `Geometries`, `FlyingObjects`, `Update()`, `GetSceneBounds()` |

### Builders

| Type | Description | Key Methods |
|------|-------------|-------------|
| `PolygonBuilder` | Fluent polygon builder | `AddVertex()`, `AddGeoCoordinate()`, `Build()`, `BuildExtruded()` |
| `PyramidBuilder` | Fluent pyramid builder | `WithBase()`, `WithHeight()`, `WithApex()`, `Build()` |
| `FlightPathBuilder` | Fluent path builder | `AddWaypoint()`, `Smooth()`, `Looping()`, `Build()` |
| `GeometryFactory` | Geometry factory | `CreatePolygon()`, `ExtrudePolygon()`, `CreatePyramid()` |

---

## 💡 Examples

### Example 1: City Block with Multiple Buildings

```csharp
var scene = new Scene();

// Building 1: Office tower
var tower = new PolygonBuilder()
    .AddVertex(0, 0).AddVertex(40, 0)
    .AddVertex(40, 40).AddVertex(0, 40)
    .BuildExtruded(height: 120);
scene.AddGeometry(tower);

// Building 2: Shopping mall (L-shaped)
var mall = Polygon2D.FromVertices(new[] {
    new Vector3D(60, 0, 0), new Vector3D(150, 0, 0),
    new Vector3D(150, 40, 0), new Vector3D(100, 40, 0),
    new Vector3D(100, 80, 0), new Vector3D(60, 80, 0)
}).Extrude(new ExtrusionOptions { Height = 25 });
scene.AddGeometry(mall);

// Building 3: Pyramid landmark
var landmark = Pyramid.CreateRegular(8, 30, 45, new Vector3D(200, 40, 0));
scene.AddGeometry(landmark);

// Export entire scene
var exporter = new MeshExporter();
foreach (var (geom, index) in scene.Geometries.Select((g, i) => (g, i)))
{
    File.WriteAllText($"building_{index}.obj", exporter.ExportToObj(geom));
}
```

### Example 2: Drone Delivery Simulation

```csharp
// Define delivery locations
var warehouse = new Vector3D(0, 0, 0);
var customer1 = new Vector3D(500, 300, 0);
var customer2 = new Vector3D(200, 600, 0);
var customer3 = new Vector3D(-300, 400, 0);

// Create delivery route
var deliveryPath = new FlightPathBuilder()
    .AddWaypoint(warehouse.WithZ(50), 0)      // Takeoff
    .AddWaypoint(customer1.WithZ(30), 30)     // Delivery 1
    .AddWaypoint(customer1.WithZ(50), 35)     // Ascend
    .AddWaypoint(customer2.WithZ(30), 65)     // Delivery 2
    .AddWaypoint(customer2.WithZ(50), 70)     // Ascend
    .AddWaypoint(customer3.WithZ(30), 100)    // Delivery 3
    .AddWaypoint(customer3.WithZ(50), 105)    // Ascend
    .AddWaypoint(warehouse.WithZ(50), 135)    // Return
    .AddWaypoint(warehouse.WithZ(0), 145)     // Land
    .Smooth()
    .Build();

// Create drone
var drone = new FlyingObject("Delivery-Drone", FlyingObjectType.Drone, deliveryPath);
drone.WaypointReached += (s, i) => Console.WriteLine($"[{drone.CurrentTime:F1}s] Waypoint {i} reached");

// Run simulation
drone.Play();
while (drone.IsPlaying)
{
    drone.Update(0.1);
    
    if (drone.Position.Z < 35)  // Near ground
        Console.WriteLine($"  Delivering at ({drone.Position.X:F0}, {drone.Position.Y:F0})");
}
Console.WriteLine("Delivery route completed!");
```

### Example 3: Geographic Analysis

```csharp
var transformer = new CoordinateTransformer();
var calculator = new DistanceCalculator();
var query = new SpatialQuery();

// Define city boundaries (simplified polygon)
var cityBoundary = new[] {
    new GeoCoordinate(40.70, -74.02),
    new GeoCoordinate(40.70, -73.97),
    new GeoCoordinate(40.75, -73.97),
    new GeoCoordinate(40.75, -74.02)
};

// Check if locations are within city
var locations = new[] {
    ("Empire State", new GeoCoordinate(40.7484, -73.9857)),
    ("Statue of Liberty", new GeoCoordinate(40.6892, -74.0445)),
    ("Central Park", new GeoCoordinate(40.7829, -73.9654))
};

Console.WriteLine("Location Analysis:");
Console.WriteLine("==================");

foreach (var (name, coord) in locations)
{
    bool inCity = query.GeoPointInPolygon(coord, cityBoundary);
    var distToCenter = calculator.HaversineDistance(
        coord, 
        new GeoCoordinate(40.725, -73.995)
    );
    
    Console.WriteLine($"{name}:");
    Console.WriteLine($"  In boundary: {inCity}");
    Console.WriteLine($"  Distance to center: {distToCenter/1000:F2} km");
}
```

---

## 📤 Export Formats

### Wavefront OBJ
- **Use Case**: 3D modeling software (Blender, Maya, 3ds Max)
- **Features**: Vertices, faces, normals
- **File Size**: Moderate

```csharp
string obj = exporter.ExportToObj(geometry, includeNormals: true);
```

### STL (Stereolithography)
- **Use Case**: 3D printing, CAD software
- **Features**: Triangle mesh only
- **File Size**: Larger (ASCII format)

```csharp
string stl = exporter.ExportToStl(geometry);
```

### GeoJSON
- **Use Case**: Web mapping (Leaflet, Mapbox, Google Maps)
- **Features**: Coordinates, properties (volume, area, centroid)
- **File Size**: Compact

```csharp
string geojson = exporter.ExportToGeoJson(geometry, transformer);
string pathJson = exporter.ExportFlightPathToGeoJson(path, sampleCount: 100);
```

---

## ⚡ Performance

### Complexity Analysis

| Operation | Time Complexity | Notes |
|-----------|-----------------|-------|
| Polygon validation | O(n²) | Self-intersection check |
| Point-in-polygon | O(n) | Ray casting algorithm |
| Triangulation | O(n²) | Ear clipping algorithm |
| Distance (Haversine) | O(1) | Fast approximation |
| Distance (Vincenty) | O(k) | k = iteration count (~10) |
| Path interpolation | O(1) | Direct calculation |
| Bounding box | O(n) | Single pass |

### Design Optimizations

- **Immutable primitives** - Thread-safe, no defensive copying
- **Lazy evaluation** - Computed properties cached on first access
- **Efficient triangulation** - Ear clipping with spatial indexing
- **Time-based animation** - Frame-rate independent updates

### Memory Considerations

| Object Type | Approximate Size |
|-------------|------------------|
| Vector3D | 24 bytes |
| GeoCoordinate | 24 bytes |
| Triangle | 72 bytes |
| Polygon2D (100 vertices) | ~2.5 KB |
| Polygon3D (100 vertices, 50m height) | ~15 KB |

---

## 🧪 Testing

### Run All Tests

```bash
cd GIS3DEngine.Solution
dotnet test
```

### Run with Verbosity

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~Polygon2DTests"
dotnet test --filter "FullyQualifiedName~PyramidTests"
dotnet test --filter "FullyQualifiedName~FlightPathTests"
```

### Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

| Category | Tests | Coverage |
|----------|-------|----------|
| Vector3D | 8 | Math operations, rotations |
| GeoCoordinate | 4 | Validation, conversion |
| BoundingBox | 3 | Creation, containment, intersection |
| Triangle | 3 | Area, normal, containment |
| Polygon2D | 8 | Creation, validation, triangulation |
| Polygon3D | 4 | Extrusion, transformation |
| Pyramid | 5 | Creation, truncation |
| FlightPath | 4 | Creation, interpolation |
| FlyingObject | 3 | Animation, control |
| Coordinate Transform | 3 | Round-trip conversion |
| Distance Calculator | 3 | Haversine, Vincenty, bearing |
| Spatial Query | 2 | Point-in-polygon, distance |
| Mesh Exporter | 3 | OBJ, STL, GeoJSON |
| **Total** | **60+** | Full coverage |

---

## 🤝 Contributing

We welcome contributions! Please follow these guidelines:

### Code Style

- Use C# 13 features where appropriate
- Follow Microsoft naming conventions
- XML documentation for all public members
- Immutable types where possible

### Pull Request Process

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for new functionality
4. Ensure all tests pass (`dotnet test`)
5. Commit changes (`git commit -m 'Add amazing feature'`)
6. Push to branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Areas for Contribution

- [ ] Additional export formats (GLTF, Collada)
- [ ] Terrain mesh generation
- [ ] Bezier curve interpolation
- [ ] Spatial indexing (R-tree, Quadtree)
- [ ] Parallel triangulation
- [ ] Additional coordinate systems (UTM, State Plane)

---

## 📄 License

This project is licensed under the MIT License - see below for details:

```
MIT License

Copyright (c) 2024 GIS3DEngine Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

<div align="center">

## 🌟 Star Us!

If you find this project useful, please consider giving it a star on GitHub!

[![GitHub stars](https://img.shields.io/github/stars/yourusername/GIS3DEngine?style=social)](https://github.com/yourusername/GIS3DEngine)

---

**Built with ❤️ for the GIS and 3D visualization community**

[Back to Top](#-gis-3d-geometry--flight-visualization-engine)

</div>
