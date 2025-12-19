using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Spatial;
using GIS3DEngine.Services;

namespace GIS3DEngine.Demo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   GIS 3D Geometry & Flight Visualization Engine Demo      ║");
        Console.WriteLine("║                    .NET Core 9 / C# 13                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Demo 1: Polygon Creation and Validation
        DemoPolygonCreation();
        
        // Demo 2: 3D Extrusion
        Demo3DExtrusion();
        
        // Demo 3: Pyramid Generation
        DemoPyramidGeneration();
        
        // Demo 4: GIS Coordinate Transformations
        DemoCoordinateTransformations();
        
        // Demo 5: Flight Path Animation
        DemoFlightAnimation();
        
        // Demo 6: Spatial Queries
        DemoSpatialQueries();
        
        // Demo 7: Export to File Formats
        DemoExport();
        
        // Demo 8: Complete Scene
        DemoCompleteScene();

        Console.WriteLine("\n✅ All demos completed successfully!");
    }

    static void DemoPolygonCreation()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("📐 DEMO 1: Polygon Creation and Validation");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Create a square polygon
        var square = new PolygonBuilder()
            .AddVertex(0, 0, 0)
            .AddVertex(100, 0, 0)
            .AddVertex(100, 100, 0)
            .AddVertex(0, 100, 0)
            .EnsureCounterClockwise()
            .Build();

        Console.WriteLine("Square Polygon:");
        Console.WriteLine($"  Vertices: {square.VertexCount}");
        Console.WriteLine($"  Area: {square.Area:N2} sq units");
        Console.WriteLine($"  Centroid: {square.Centroid}");
        Console.WriteLine($"  Is Convex: {square.IsConvex}");
        Console.WriteLine($"  Winding Order: {square.WindingOrder}");

        // Create a regular hexagon
        var hexagon = Polygon2D.CreateRegular(6, 50);
        Console.WriteLine($"\nRegular Hexagon:");
        Console.WriteLine($"  Vertices: {hexagon.VertexCount}");
        Console.WriteLine($"  Area: {hexagon.Area:N2} sq units");
        Console.WriteLine($"  Is Convex: {hexagon.IsConvex}");

        // Validate a polygon
        var validator = new PolygonValidator();
        var validation = validator.Validate(square.Vertices);
        Console.WriteLine($"\nValidation Result:");
        Console.WriteLine($"  Valid: {validation.IsValid}");
        Console.WriteLine($"  Convex: {validation.IsConvex}");
        Console.WriteLine($"  Self-Intersecting: {validation.HasSelfIntersection}");

        // Create concave polygon (L-shape)
        var lShape = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(100, 0, 0),
            new Vector3D(100, 50, 0),
            new Vector3D(50, 50, 0),
            new Vector3D(50, 100, 0),
            new Vector3D(0, 100, 0)
        });
        Console.WriteLine($"\nL-Shape Polygon:");
        Console.WriteLine($"  Vertices: {lShape.VertexCount}");
        Console.WriteLine($"  Is Convex: {lShape.IsConvex}");
        Console.WriteLine($"  Area: {lShape.Area:N2} sq units");
        Console.WriteLine();
    }

    static void Demo3DExtrusion()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🏗️  DEMO 2: 3D Extrusion");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var square = Polygon2D.CreateRegular(4, 50);

        // Simple prism
        var prism = square.Extrude(new ExtrusionOptions { Height = 100 });
        Console.WriteLine("Prism (Square base, 100 height):");
        Console.WriteLine($"  Volume: {prism.Volume:N2} cubic units");
        Console.WriteLine($"  Surface Area: {prism.SurfaceArea:N2} sq units");
        Console.WriteLine($"  Triangles: {prism.Triangles.Count}");
        Console.WriteLine($"  Bounds: {prism.Bounds}");

        // Tapered extrusion (frustum)
        var frustum = square.Extrude(new ExtrusionOptions 
        { 
            Height = 100, 
            TopScale = 0.5 
        });
        Console.WriteLine($"\nFrustum (50% top scale):");
        Console.WriteLine($"  Volume: {frustum.Volume:N2} cubic units");
        Console.WriteLine($"  Surface Area: {frustum.SurfaceArea:N2} sq units");

        // Rotated extrusion
        var rotated = square.Extrude(new ExtrusionOptions
        {
            Height = 80,
            Rotation = new Vector3D(0, 0, Math.PI / 8) // 22.5 degrees
        });
        Console.WriteLine($"\nRotated Extrusion (22.5° Z rotation):");
        Console.WriteLine($"  Volume: {rotated.Volume:N2} cubic units");
        Console.WriteLine();
    }

    static void DemoPyramidGeneration()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔺 DEMO 3: Pyramid Generation");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Square pyramid (like Egyptian pyramid)
        var squarePyramid = Pyramid.CreateSquarePyramid(100, 75);
        Console.WriteLine("Square Pyramid (100x100 base, 75 height):");
        Console.WriteLine($"  Volume: {squarePyramid.Volume:N2} cubic units");
        Console.WriteLine($"  Surface Area: {squarePyramid.SurfaceArea:N2} sq units");
        Console.WriteLine($"  Slant Height: {squarePyramid.SlantHeight:N2} units");
        Console.WriteLine($"  Lateral Faces: {squarePyramid.LateralFaceCount}");
        Console.WriteLine($"  Is Regular: {squarePyramid.IsRegular}");

        // Regular hexagonal pyramid
        var hexPyramid = Pyramid.CreateRegular(6, 40, 60);
        Console.WriteLine($"\nHexagonal Pyramid:");
        Console.WriteLine($"  Volume: {hexPyramid.Volume:N2} cubic units");
        Console.WriteLine($"  Lateral Faces: {hexPyramid.LateralFaceCount}");

        // Tetrahedron
        var tetrahedron = Pyramid.CreateTetrahedron(50);
        Console.WriteLine($"\nTetrahedron (equilateral, side=50):");
        Console.WriteLine($"  Volume: {tetrahedron.Volume:N2} cubic units");
        Console.WriteLine($"  Surface Area: {tetrahedron.SurfaceArea:N2} sq units");

        // Pyramid with custom apex
        var customApex = new PolygonBuilder()
            .AddVertex(0, 0, 0)
            .AddVertex(100, 0, 0)
            .AddVertex(100, 100, 0)
            .AddVertex(0, 100, 0)
            .Build();
        var offsetPyramid = Pyramid.CreateWithApex(customApex, new Vector3D(25, 25, 80));
        Console.WriteLine($"\nOff-center Pyramid (apex at 25,25,80):");
        Console.WriteLine($"  Is Regular: {offsetPyramid.IsRegular}");
        Console.WriteLine($"  Volume: {offsetPyramid.Volume:N2} cubic units");

        // Truncate pyramid to create frustum
        var truncated = squarePyramid.Truncate(0.6);
        Console.WriteLine($"\nTruncated Pyramid (60% height):");
        Console.WriteLine($"  Volume: {truncated.Volume:N2} cubic units");
        Console.WriteLine();
    }

    static void DemoCoordinateTransformations()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🌍 DEMO 4: GIS Coordinate Transformations");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var transformer = new CoordinateTransformer();
        var calculator = new DistanceCalculator();

        // Define some GPS coordinates
        var newYork = new GeoCoordinate(40.7128, -74.0060, 10);
        var losAngeles = new GeoCoordinate(34.0522, -118.2437, 71);
        var london = new GeoCoordinate(51.5074, -0.1278, 11);

        Console.WriteLine("GPS Coordinates:");
        Console.WriteLine($"  New York: {newYork}");
        Console.WriteLine($"  Los Angeles: {losAngeles}");
        Console.WriteLine($"  London: {london}");

        // Convert to ECEF
        var nyECEF = transformer.GeoToCartesian(newYork);
        Console.WriteLine($"\nNew York in ECEF (meters):");
        Console.WriteLine($"  X: {nyECEF.X:N2}, Y: {nyECEF.Y:N2}, Z: {nyECEF.Z:N2}");

        // Round-trip conversion
        var nyBack = transformer.CartesianToGeo(nyECEF);
        Console.WriteLine($"  Round-trip back: {nyBack}");

        // Distance calculations
        var distNYtoLA = calculator.HaversineDistance(newYork, losAngeles);
        var distNYtoLondon = calculator.VincentyDistance(newYork, london);
        Console.WriteLine($"\nDistances:");
        Console.WriteLine($"  NY to LA (Haversine): {distNYtoLA / 1000:N2} km");
        Console.WriteLine($"  NY to London (Vincenty): {distNYtoLondon / 1000:N2} km");

        // Bearing
        var bearing = calculator.InitialBearing(newYork, london);
        Console.WriteLine($"\nBearing NY to London: {bearing * 180 / Math.PI:N2}° (from North)");

        // Local tangent plane
        var (east, north, up) = transformer.GetLocalTangentPlane(newYork);
        Console.WriteLine($"\nLocal Tangent Plane at New York (ENU):");
        Console.WriteLine($"  East:  {east}");
        Console.WriteLine($"  North: {north}");
        Console.WriteLine($"  Up:    {up}");

        // ENU conversion
        var laInNYFrame = transformer.GeoToENU(losAngeles, newYork);
        Console.WriteLine($"\nLos Angeles relative to New York (ENU meters):");
        Console.WriteLine($"  East: {laInNYFrame.X:N2}, North: {laInNYFrame.Y:N2}, Up: {laInNYFrame.Z:N2}");
        Console.WriteLine();
    }

    static void DemoFlightAnimation()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("✈️  DEMO 5: Flight Path Animation");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Create a simple flight path
        var linearPath = new FlightPathBuilder()
            .AddWaypoint(new Vector3D(0, 0, 100), 0)
            .AddWaypoint(new Vector3D(500, 0, 150), 10)
            .AddWaypoint(new Vector3D(1000, 500, 200), 25)
            .AddWaypoint(new Vector3D(500, 1000, 150), 40)
            .AddWaypoint(new Vector3D(0, 500, 100), 55)
            .Linear()
            .Build();

        Console.WriteLine("Linear Flight Path:");
        Console.WriteLine($"  Waypoints: {linearPath.Waypoints.Count}");
        Console.WriteLine($"  Total Duration: {linearPath.TotalDuration} seconds");
        Console.WriteLine($"  Total Distance: {linearPath.TotalDistance:N2} units");

        // Sample positions along the path
        Console.WriteLine("\n  Sample positions:");
        for (double t = 0; t <= linearPath.TotalDuration; t += 10)
        {
            var pos = linearPath.GetPositionAtTime(t);
            var heading = linearPath.GetHeadingAtTime(t) * 180 / Math.PI;
            Console.WriteLine($"    t={t:N0}s: Position={pos}, Heading={heading:N1}°");
        }

        // Create smooth spline path
        var splinePath = new FlightPathBuilder()
            .AddWaypoint(new Vector3D(0, 0, 100), 0)
            .AddWaypoint(new Vector3D(500, 0, 150), 10)
            .AddWaypoint(new Vector3D(1000, 500, 200), 25)
            .Smooth()
            .Looping(true)
            .Build();

        Console.WriteLine($"\nSmooth Spline Path:");
        Console.WriteLine($"  Looping: {splinePath.IsLooping}");
        Console.WriteLine($"  Interpolation: {splinePath.InterpolationType}");

        // Create orbit path
        var orbitPath = FlightPath.CreateOrbit(
            center: new Vector3D(500, 500, 0),
            radius: 200,
            altitude: 100,
            duration: 60
        );
        Console.WriteLine($"\nOrbit Path (radius=200, alt=100):");
        Console.WriteLine($"  Duration: {orbitPath.TotalDuration} seconds");
        Console.WriteLine($"  Distance: {orbitPath.TotalDistance:N2} units");

        // Create a flying object
        var drone = new FlyingObject("Drone-001", FlyingObjectType.Drone, linearPath);
        drone.Name = "Survey Drone";
        drone.MaxSpeed = 50;

        Console.WriteLine($"\nFlying Object:");
        Console.WriteLine($"  ID: {drone.Id}");
        Console.WriteLine($"  Name: {drone.Name}");
        Console.WriteLine($"  Type: {drone.Type}");
        Console.WriteLine($"  Initial Position: {drone.Position}");

        // Simulate a few updates
        drone.Play();
        Console.WriteLine("\n  Simulation (10 steps of 2s each):");
        for (int i = 0; i < 10; i++)
        {
            drone.Update(2.0);
            Console.WriteLine($"    Step {i + 1}: Pos={drone.Position}, Speed={drone.Velocity.Magnitude:N2}");
        }
        Console.WriteLine();
    }

    static void DemoSpatialQueries()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔍 DEMO 6: Spatial Queries");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var query = new SpatialQuery();

        // Create a test polygon
        var polygon = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(100, 0, 0),
            new Vector3D(100, 100, 0),
            new Vector3D(0, 100, 0)
        });

        // Point in polygon tests
        var testPoints = new[]
        {
            (new Vector3D(50, 50, 0), "Center"),
            (new Vector3D(10, 10, 0), "Inside near corner"),
            (new Vector3D(150, 50, 0), "Outside right"),
            (new Vector3D(-10, 50, 0), "Outside left"),
            (new Vector3D(0, 0, 0), "On corner")
        };

        Console.WriteLine("Point in Polygon Tests (100x100 square):");
        foreach (var (point, desc) in testPoints)
        {
            var inside = query.PointInPolygon(point, polygon.Vertices);
            Console.WriteLine($"  {desc} {point}: {(inside ? "INSIDE" : "OUTSIDE")}");
        }

        // Distance to edge
        var distPoint = new Vector3D(50, 120, 0);
        var distToEdge = query.DistanceToPolygonEdge(distPoint, polygon.Vertices);
        Console.WriteLine($"\nDistance from {distPoint} to polygon edge: {distToEdge:N2} units");

        // Closest point on edge
        var closest = query.ClosestPointOnPolygonEdge(distPoint, polygon.Vertices);
        Console.WriteLine($"Closest point on edge: {closest}");

        // Height above polygon
        var extruded = polygon.Extrude(new ExtrusionOptions { Height = 50 });
        var heightPoint = new Vector3D(50, 50, 75);
        var height = query.HeightAbovePolygon(heightPoint, extruded);
        Console.WriteLine($"\nHeight of point (50,50,75) above base: {height:N2} units");

        // Polygon intersection test
        var poly1Verts = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(100, 0, 0),
            new Vector3D(100, 100, 0),
            new Vector3D(0, 100, 0)
        };
        var poly2Verts = new[]
        {
            new Vector3D(50, 50, 0),
            new Vector3D(150, 50, 0),
            new Vector3D(150, 150, 0),
            new Vector3D(50, 150, 0)
        };
        var poly3Verts = new[]
        {
            new Vector3D(200, 0, 0),
            new Vector3D(300, 0, 0),
            new Vector3D(300, 100, 0),
            new Vector3D(200, 100, 0)
        };

        Console.WriteLine("\nPolygon Intersection Tests:");
        Console.WriteLine($"  Poly1 vs Poly2 (overlapping): {query.PolygonsIntersect(poly1Verts, poly2Verts)}");
        Console.WriteLine($"  Poly1 vs Poly3 (separate): {query.PolygonsIntersect(poly1Verts, poly3Verts)}");
        Console.WriteLine();
    }

    static void DemoExport()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("📤 DEMO 7: Export to File Formats");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var exporter = new MeshExporter();

        // Create a pyramid for export
        var pyramid = Pyramid.CreateSquarePyramid(50, 40);

        // Export to OBJ
        var obj = exporter.ExportToObj(pyramid);
        Console.WriteLine("OBJ Export (first 20 lines):");
        var objLines = obj.Split('\n').Take(20);
        foreach (var line in objLines)
        {
            Console.WriteLine($"  {line}");
        }
        Console.WriteLine("  ...");

        // Export to STL
        var stl = exporter.ExportToStl(pyramid);
        Console.WriteLine("\nSTL Export (first 15 lines):");
        var stlLines = stl.Split('\n').Take(15);
        foreach (var line in stlLines)
        {
            Console.WriteLine($"  {line}");
        }
        Console.WriteLine("  ...");

        // Export to GeoJSON
        var geojson = exporter.ExportToGeoJson(pyramid);
        Console.WriteLine("\nGeoJSON Export:");
        Console.WriteLine(geojson);

        // Export flight path to GeoJSON
        var path = FlightPath.CreateOrbit(Vector3D.Zero, 100, 50, 60);
        var pathJson = exporter.ExportFlightPathToGeoJson(path, 12);
        Console.WriteLine("\nFlight Path GeoJSON (12 samples):");
        Console.WriteLine(pathJson);
        Console.WriteLine();
    }

    static void DemoCompleteScene()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🎬 DEMO 8: Complete Scene with Animation");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var scene = new Scene();

        // Add buildings (extruded polygons)
        var building1 = new PolygonBuilder()
            .AddVertex(0, 0, 0)
            .AddVertex(50, 0, 0)
            .AddVertex(50, 50, 0)
            .AddVertex(0, 50, 0)
            .BuildExtruded(100);
        scene.AddGeometry(building1);

        var building2 = new PolygonBuilder()
            .AddVertex(100, 0, 0)
            .AddVertex(180, 0, 0)
            .AddVertex(180, 60, 0)
            .AddVertex(100, 60, 0)
            .BuildExtruded(75);
        scene.AddGeometry(building2);

        // Add a pyramid landmark
        var landmark = Pyramid.CreateRegular(6, 30, 50, new Vector3D(250, 30, 0));
        scene.AddGeometry(landmark);

        Console.WriteLine("Scene Contents:");
        Console.WriteLine($"  Geometries: {scene.Geometries.Count}");
        foreach (var geom in scene.Geometries)
        {
            Console.WriteLine($"    - Volume: {geom.Volume:N2}, Center: {geom.Centroid}");
        }

        // Create a tour flight path visiting all geometries
        var tourPath = FlightPathBuilder.CreateTour(scene.Geometries, 50, 20);
        var tourDrone = scene.CreateFlyingObject("Tour-Drone", FlyingObjectType.Drone, tourPath);
        tourDrone.Name = "City Tour Drone";

        Console.WriteLine($"\nTour Flight Path:");
        Console.WriteLine($"  Distance: {tourPath.TotalDistance:N2} units");
        Console.WriteLine($"  Duration: {tourPath.TotalDuration:N2} seconds");

        // Scene bounds
        var bounds = scene.GetSceneBounds();
        Console.WriteLine($"\nScene Bounds:");
        Console.WriteLine($"  Min: {bounds.Min}");
        Console.WriteLine($"  Max: {bounds.Max}");
        Console.WriteLine($"  Size: {bounds.Size}");

        // Simulate scene
        Console.WriteLine("\nRunning simulation (5 steps):");
        scene.StartSimulation();
        for (int i = 0; i < 5; i++)
        {
            scene.Update(5.0);
            Console.WriteLine($"  Step {i + 1}: Drone at {tourDrone.Position}");
        }
        scene.StopSimulation();
        Console.WriteLine();
    }
}
