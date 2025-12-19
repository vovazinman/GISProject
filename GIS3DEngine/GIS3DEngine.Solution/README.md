# GIS 3D Geometry & Flight Visualization Engine

[![.NET Core](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A comprehensive GIS-oriented 3D visualization system built with .NET Core 9 and C# 13, demonstrating strong understanding of computational geometry, spatial data, and animation logic.

## Features

### 🔷 Polygon Creation (2D → 3D)
- Geographic 2D polygons with lat/lon or planar coordinates
- Convex and non-convex polygon support
- Validation (winding order, self-intersection detection)
- Extrusion to 3D with configurable height, scale, and rotation
- Pyramid generation with polygon base

### 🔺 Pyramid Geometry
- Regular and irregular pyramids
- Configurable apex position
- Face normals and edge vectors
- Surface triangulation for rendering
- GIS coordinate system compatibility

### ✈️ Flying Objects & Animation
- Movement along predefined paths
- Linear and curved (Catmull-Rom spline) interpolation
- Time-based updates (frame-independent)
- Speed control and altitude changes
- Orbit and figure-eight path generation

### 🌍 GIS & Spatial Logic
- WGS84 ↔ ECEF ↔ ENU coordinate transformations
- Haversine and Vincenty distance calculations
- Point-in-polygon checks
- Distance and bearing calculations
- Polygon intersection detection

## Project Structure

```
GIS3DEngine.Solution/
├── GIS3DEngine.sln
├── global.json
├── README.md
├── src/
│   ├── GIS3DEngine.Core/           # Core geometry and spatial library
│   │   ├── Primitives/             # Vector3D, GeoCoordinate, BoundingBox, etc.
│   │   ├── Interfaces/             # IGeometry3D, IAnimationPath, etc.
│   │   ├── Geometry/               # Polygon2D, Polygon3D, Pyramid
│   │   ├── Animation/              # FlightPath, FlyingObject, Interpolators
│   │   └── Spatial/                # CoordinateTransformer, DistanceCalculator
│   ├── GIS3DEngine.Services/       # High-level services and builders
│   │   └── Services.cs             # GeometryFactory, Builders, Exporters
│   └── GIS3DEngine.Demo/           # Demo console application
│       └── Program.cs
└── tests/
    └── GIS3DEngine.Tests/          # Unit tests (xUnit)
        └── UnitTests.cs
```

## Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build and Run

```bash
# Clone or download the solution
cd GIS3DEngine.Solution

# Build
dotnet build

# Run the demo
dotnet run --project src/GIS3DEngine.Demo

# Run tests
dotnet test
```

## Usage Examples

### Create a Polygon and Extrude to 3D

```csharp
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Services;

// Using the fluent builder
var building = new PolygonBuilder()
    .AddVertex(0, 0, 0)
    .AddVertex(50, 0, 0)
    .AddVertex(50, 50, 0)
    .AddVertex(0, 50, 0)
    .BuildExtruded(height: 100);

Console.WriteLine($"Volume: {building.Volume:N2} m³");
Console.WriteLine($"Surface Area: {building.SurfaceArea:N2} m²");
```

### Create from Geographic Coordinates

```csharp
var geoBuilding = new PolygonBuilder()
    .AddGeoCoordinate(40.7128, -74.0060, 0)  // NYC
    .AddGeoCoordinate(40.7130, -74.0060, 0)
    .AddGeoCoordinate(40.7130, -74.0055, 0)
    .AddGeoCoordinate(40.7128, -74.0055, 0)
    .BuildExtruded(height: 200);
```

### Create a Pyramid

```csharp
// Square pyramid
var pyramid = Pyramid.CreateSquarePyramid(sideLength: 100, height: 75);

// Regular hexagonal pyramid
var hexPyramid = Pyramid.CreateRegular(sides: 6, radius: 40, height: 60);

// Tetrahedron
var tetrahedron = Pyramid.CreateTetrahedron(sideLength: 50);

// Truncate to frustum
var frustum = pyramid.Truncate(relativeHeight: 0.6);
```

### Flight Path Animation

```csharp
// Create a flight path
var path = new FlightPathBuilder()
    .AddWaypoint(new Vector3D(0, 0, 100), time: 0)
    .AddWaypoint(new Vector3D(500, 0, 150), time: 10)
    .AddWaypoint(new Vector3D(1000, 500, 200), time: 25)
    .Smooth()  // Use Catmull-Rom spline
    .Build();

// Create a flying object
var drone = new FlyingObject("Drone-001", FlyingObjectType.Drone, path);
drone.Play();

// Update in your game loop
drone.Update(deltaTime: 0.016);  // ~60 FPS
Console.WriteLine($"Position: {drone.Position}");
Console.WriteLine($"Heading: {drone.Heading * 180 / Math.PI:N1}°");
```

### Orbit Path

```csharp
var orbit = FlightPath.CreateOrbit(
    center: new Vector3D(500, 500, 0),
    radius: 200,
    altitude: 100,
    duration: 60  // seconds
);
```

### Coordinate Transformations

```csharp
var transformer = new CoordinateTransformer();

// WGS84 to ECEF
var nyc = new GeoCoordinate(40.7128, -74.0060, 10);
var ecef = transformer.GeoToCartesian(nyc);

// Convert back
var geoBack = transformer.CartesianToGeo(ecef);

// Get local tangent plane (ENU)
var (east, north, up) = transformer.GetLocalTangentPlane(nyc);
```

### Distance Calculations

```csharp
var calculator = new DistanceCalculator();

var nyc = new GeoCoordinate(40.7128, -74.0060);
var london = new GeoCoordinate(51.5074, -0.1278);

// Haversine (fast)
var distH = calculator.HaversineDistance(nyc, london);

// Vincenty (accurate)
var distV = calculator.VincentyDistance(nyc, london);

// Bearing
var bearing = calculator.InitialBearing(nyc, london);
Console.WriteLine($"Distance: {distV / 1000:N0} km");
Console.WriteLine($"Bearing: {bearing * 180 / Math.PI:N1}°");
```

### Spatial Queries

```csharp
var query = new SpatialQuery();

var polygon = new[] {
    new Vector3D(0, 0, 0),
    new Vector3D(100, 0, 0),
    new Vector3D(100, 100, 0),
    new Vector3D(0, 100, 0)
};

// Point in polygon
bool inside = query.PointInPolygon(new Vector3D(50, 50, 0), polygon);

// Distance to edge
double dist = query.DistanceToPolygonEdge(new Vector3D(50, 150, 0), polygon);
```

### Export to File Formats

```csharp
var exporter = new MeshExporter();
var pyramid = Pyramid.CreateSquarePyramid(50, 40);

// Wavefront OBJ
string obj = exporter.ExportToObj(pyramid);
File.WriteAllText("pyramid.obj", obj);

// STL
string stl = exporter.ExportToStl(pyramid);
File.WriteAllText("pyramid.stl", stl);

// GeoJSON
string geojson = exporter.ExportToGeoJson(pyramid);
File.WriteAllText("pyramid.geojson", geojson);
```

### Complete Scene

```csharp
var scene = new Scene();

// Add geometries
scene.AddGeometry(new PolygonBuilder()
    .AddVertex(0, 0).AddVertex(50, 0).AddVertex(50, 50).AddVertex(0, 50)
    .BuildExtruded(100));

scene.AddGeometry(Pyramid.CreateRegular(6, 30, 50, new Vector3D(100, 100, 0)));

// Create tour path
var tourPath = FlightPathBuilder.CreateTour(scene.Geometries, altitude: 50, speed: 20);
var tourDrone = scene.CreateFlyingObject("Tour", FlyingObjectType.Drone, tourPath);

// Run simulation
scene.StartSimulation();
while (tourDrone.IsPlaying)
{
    scene.Update(deltaTime: 0.1);
    Console.WriteLine($"Drone at: {tourDrone.Position}");
}
```

## Architecture

### Core Components

| Component | Description |
|-----------|-------------|
| `Vector3D` | Immutable 3D vector with full math operations |
| `GeoCoordinate` | WGS84 coordinate (lat, lon, alt) |
| `Polygon2D` | 2D polygon with validation and triangulation |
| `Polygon3D` | Extruded 3D polygon |
| `Pyramid` | Pyramidal geometry |
| `FlightPath` | Time-based animation path |
| `FlyingObject` | Animated entity following paths |

### Spatial Services

| Service | Description |
|---------|-------------|
| `CoordinateTransformer` | WGS84 ↔ ECEF ↔ ENU transformations |
| `DistanceCalculator` | Haversine/Vincenty distance, bearing |
| `SpatialQuery` | Point-in-polygon, intersection tests |
| `PolygonValidator` | Convexity, self-intersection checks |

### Builders & Factories

| Builder | Description |
|---------|-------------|
| `PolygonBuilder` | Fluent polygon construction |
| `PyramidBuilder` | Fluent pyramid construction |
| `FlightPathBuilder` | Fluent path construction |
| `GeometryFactory` | Factory for geometry creation |
| `MeshExporter` | OBJ, STL, GeoJSON export |

## Validation Rules

| Rule | Complexity |
|------|------------|
| Minimum vertices (≥3) | O(1) |
| Winding order detection | O(n) |
| Convexity check | O(n) |
| Self-intersection | O(n²) |
| Point-in-polygon | O(n) |

## Export Formats

| Format | Use Case |
|--------|----------|
| OBJ | 3D modeling software (Blender, Maya) |
| STL | 3D printing |
| GeoJSON | Web mapping (Leaflet, Mapbox) |

## Performance Considerations

- Immutable primitives for thread safety
- Lazy evaluation of computed properties
- Efficient triangulation using ear clipping
- Time-based animation (frame-independent)

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~Polygon2DTests"
```

## License

MIT License - See LICENSE file for details.

## References

- [WGS84 Geodetic System](https://en.wikipedia.org/wiki/World_Geodetic_System)
- [Vincenty's Formulae](https://en.wikipedia.org/wiki/Vincenty%27s_formulae)
- [Ear Clipping Triangulation](https://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf)
- [Catmull-Rom Splines](https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline)
