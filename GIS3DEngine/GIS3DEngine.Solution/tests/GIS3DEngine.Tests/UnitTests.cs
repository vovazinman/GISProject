using Xunit;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Spatial;
using GIS3DEngine.Services;
using GIS3DEngine.Core.Flights;

namespace GIS3DEngine.Tests;

public class Vector3DTests
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var v = new Vector3D(1, 2, 3);
        Assert.Equal(1, v.X);
        Assert.Equal(2, v.Y);
        Assert.Equal(3, v.Z);
    }

    [Fact]
    public void Magnitude_CalculatesCorrectly()
    {
        var v = new Vector3D(3, 4, 0);
        Assert.Equal(5, v.Magnitude, 6);
    }

    [Fact]
    public void Normalized_ReturnsUnitVector()
    {
        var v = new Vector3D(3, 4, 0);
        var n = v.Normalized;
        Assert.Equal(1, n.Magnitude, 6);
    }

    [Fact]
    public void Dot_CalculatesCorrectly()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);
        Assert.Equal(32, Vector3D.Dot(a, b), 6);
    }

    [Fact]
    public void Cross_CalculatesCorrectly()
    {
        var a = Vector3D.UnitX;
        var b = Vector3D.UnitY;
        var c = Vector3D.Cross(a, b);
        Assert.Equal(Vector3D.UnitZ, c);
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var a = new Vector3D(0, 0, 0);
        var b = new Vector3D(10, 10, 10);
        var mid = Vector3D.Lerp(a, b, 0.5);
        Assert.Equal(new Vector3D(5, 5, 5), mid);
    }

    [Fact]
    public void Distance_CalculatesCorrectly()
    {
        var a = new Vector3D(0, 0, 0);
        var b = new Vector3D(3, 4, 0);
        Assert.Equal(5, Vector3D.Distance(a, b), 6);
    }

    [Fact]
    public void Operators_WorkCorrectly()
    {
        var a = new Vector3D(1, 2, 3);
        var b = new Vector3D(4, 5, 6);
        
        Assert.Equal(new Vector3D(5, 7, 9), a + b);
        Assert.Equal(new Vector3D(-3, -3, -3), a - b);
        Assert.Equal(new Vector3D(2, 4, 6), a * 2);
        Assert.Equal(new Vector3D(0.5, 1, 1.5), a / 2);
    }

    [Fact]
    public void RotateZ_RotatesCorrectly()
    {
        var v = Vector3D.UnitX;
        var rotated = v.RotateZ(Math.PI / 2);
        Assert.Equal(0, rotated.X, 6);
        Assert.Equal(1, rotated.Y, 6);
        Assert.Equal(0, rotated.Z, 6);
    }
}

public class GeoCoordinateTests
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var c = new GeoCoordinate(40.7128, -74.0060, 10);
        Assert.Equal(40.7128, c.Latitude);
        Assert.Equal(-74.0060, c.Longitude);
        Assert.Equal(10, c.Altitude);
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidLatitude()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(91, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(-91, 0));
    }

    [Fact]
    public void Constructor_ThrowsOnInvalidLongitude()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0, 181));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(0, -181));
    }

    [Fact]
    public void RadiansConversion_IsCorrect()
    {
        var c = new GeoCoordinate(180, 90);
        Assert.Equal(Math.PI, c.LatitudeRadians, 6);
        Assert.Equal(Math.PI / 2, c.LongitudeRadians, 6);
    }
}

public class BoundingBoxTests
{
    [Fact]
    public void FromPoints_CreatesCorrectBounds()
    {
        var points = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 5, 3),
            new Vector3D(-5, 8, -2)
        };
        var box = BoundingBox.FromPoints(points);
        
        Assert.Equal(-5, box.Min.X);
        Assert.Equal(0, box.Min.Y);
        Assert.Equal(-2, box.Min.Z);
        Assert.Equal(10, box.Max.X);
        Assert.Equal(8, box.Max.Y);
        Assert.Equal(3, box.Max.Z);
    }

    [Fact]
    public void Contains_WorksCorrectly()
    {
        var box = new BoundingBox(new Vector3D(0, 0, 0), new Vector3D(10, 10, 10));
        Assert.True(box.Contains(new Vector3D(5, 5, 5)));
        Assert.False(box.Contains(new Vector3D(15, 5, 5)));
    }

    [Fact]
    public void Intersects_WorksCorrectly()
    {
        var box1 = new BoundingBox(new Vector3D(0, 0, 0), new Vector3D(10, 10, 10));
        var box2 = new BoundingBox(new Vector3D(5, 5, 5), new Vector3D(15, 15, 15));
        var box3 = new BoundingBox(new Vector3D(20, 20, 20), new Vector3D(30, 30, 30));
        
        Assert.True(box1.Intersects(box2));
        Assert.False(box1.Intersects(box3));
    }
}

public class TriangleTests
{
    [Fact]
    public void Area_CalculatesCorrectly()
    {
        var tri = new Triangle(
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(0, 10, 0)
        );
        Assert.Equal(50, tri.Area, 6);
    }

    [Fact]
    public void Normal_CalculatesCorrectly()
    {
        var tri = new Triangle(
            new Vector3D(0, 0, 0),
            new Vector3D(1, 0, 0),
            new Vector3D(0, 1, 0)
        );
        var normal = tri.Normal;
        Assert.Equal(0, normal.X, 6);
        Assert.Equal(0, normal.Y, 6);
        Assert.Equal(1, normal.Z, 6);
    }

    [Fact]
    public void Contains_WorksCorrectly()
    {
        var tri = new Triangle(
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(5, 10, 0)
        );
        Assert.True(tri.Contains(new Vector3D(5, 3, 0)));
        Assert.False(tri.Contains(new Vector3D(0, 10, 0)));
    }
}

public class Polygon2DTests
{
    [Fact]
    public void FromVertices_CreatesPolygon()
    {
        var poly = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        });
        Assert.Equal(4, poly.VertexCount);
    }

    [Fact]
    public void CreateRegular_CreatesCorrectPolygon()
    {
        var hexagon = Polygon2D.CreateRegular(6, 10);
        Assert.Equal(6, hexagon.VertexCount);
        Assert.True(hexagon.IsConvex);
    }

    [Fact]
    public void Area_CalculatesCorrectly()
    {
        var square = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        });
        Assert.Equal(100, square.Area, 6);
    }

    [Fact]
    public void IsConvex_DetectsConvexPolygon()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        Assert.True(square.IsConvex);
    }

    [Fact]
    public void IsConvex_DetectsConcavePolygon()
    {
        var lShape = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 5, 0),
            new Vector3D(5, 5, 0),
            new Vector3D(5, 10, 0),
            new Vector3D(0, 10, 0)
        });
        Assert.False(lShape.IsConvex);
    }

    [Fact]
    public void WindingOrder_DetectsCorrectly()
    {
        var ccw = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        });
        Assert.Equal(WindingOrder.CounterClockwise, ccw.WindingOrder);
    }

    [Fact]
    public void ContainsPoint_WorksCorrectly()
    {
        var square = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        });
        Assert.True(square.ContainsPoint(new Vector3D(5, 5, 0)));
        Assert.False(square.ContainsPoint(new Vector3D(15, 5, 0)));
    }

    [Fact]
    public void Triangulate_CreatesValidTriangles()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        var triangles = square.Triangulate();
        Assert.Equal(2, triangles.Count);
    }

    [Fact]
    public void IsSelfIntersecting_DetectsIntersection()
    {
        // Bowtie shape (self-intersecting)
        var bowtie = Polygon2D.FromVertices(new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(0, 10, 0)
        });
        Assert.True(bowtie.IsSelfIntersecting());
    }
}

public class Polygon3DTests
{
    [Fact]
    public void Extrude_CreatesValidGeometry()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        var prism = square.Extrude(new ExtrusionOptions { Height = 20 });
        
        Assert.Equal(8, prism.Vertices.Count);
        Assert.True(prism.Volume > 0);
        Assert.True(prism.SurfaceArea > 0);
    }

    [Fact]
    public void Extrude_WithTopScale_CreatesFrustum()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        var frustum = square.Extrude(new ExtrusionOptions { Height = 20, TopScale = 0.5 });
        
        Assert.Equal(8, frustum.Vertices.Count);
    }

    [Fact]
    public void Translate_MovesGeometry()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        var prism = square.Extrude(new ExtrusionOptions { Height = 20 });
        var moved = prism.Translate(new Vector3D(100, 100, 0));
        
        Assert.True(moved.Centroid.X > prism.Centroid.X);
    }

    [Fact]
    public void Triangulate_ReturnsTriangles()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        var prism = square.Extrude(new ExtrusionOptions { Height = 20 });
        var triangles = prism.Triangulate();
        
        Assert.True(triangles.Count > 0);
    }
}

public class PyramidTests
{
    [Fact]
    public void Create_CreatesValidPyramid()
    {
        var square = Polygon2D.CreateRegular(4, 10);
        var pyramid = Pyramid.Create(square, 15);
        
        Assert.Equal(5, pyramid.Vertices.Count); // 4 base + 1 apex
        Assert.True(pyramid.Volume > 0);
    }

    [Fact]
    public void CreateRegular_CreatesSymmetricPyramid()
    {
        var pyramid = Pyramid.CreateRegular(4, 10, 15);
        Assert.True(pyramid.IsRegular);
        Assert.Equal(4, pyramid.LateralFaceCount);
    }

    [Fact]
    public void CreateSquarePyramid_CalculatesVolumeCorrectly()
    {
        var pyramid = Pyramid.CreateSquarePyramid(10, 15);
        var expectedVolume = (10 * 10 * 15) / 3.0;
        Assert.Equal(expectedVolume, pyramid.Volume, 1);
    }

    [Fact]
    public void CreateTetrahedron_HasFourFaces()
    {
        var tetra = Pyramid.CreateTetrahedron(10);
        Assert.Equal(3, tetra.LateralFaceCount);
    }

    [Fact]
    public void Truncate_CreatesFrustum()
    {
        var pyramid = Pyramid.CreateSquarePyramid(10, 20);
        var frustum = pyramid.Truncate(0.5);
        
        Assert.True(frustum.Volume < pyramid.Volume);
    }
}

public class FlightPathTests
{
    [Fact]
    public void CreateLinear_CreatesPath()
    {
        var waypoints = new[]
        {
            new Waypoint(new Vector3D(0, 0, 0), 0),
            new Waypoint(new Vector3D(100, 0, 0), 10)
        };
        var path = FlightPath.CreateLinear(waypoints);
        
        Assert.Equal(2, path.Waypoints.Count);
        Assert.Equal(10, path.TotalDuration);
        Assert.Equal(100, path.TotalDistance, 6);
    }

    [Fact]
    public void GetPositionAtTime_InterpolatesCorrectly()
    {
        var waypoints = new[]
        {
            new Waypoint(new Vector3D(0, 0, 0), 0),
            new Waypoint(new Vector3D(100, 0, 0), 10)
        };
        var path = FlightPath.CreateLinear(waypoints);
        
        var mid = path.GetPositionAtTime(5);
        Assert.Equal(50, mid.X, 6);
    }

    [Fact]
    public void CreateOrbit_CreatesCircularPath()
    {
        var path = FlightPath.CreateOrbit(Vector3D.Zero, 100, 50, 60);
        Assert.True(path.IsLooping);
        Assert.Equal(60, path.TotalDuration);
    }

    [Fact]
    public void GetVelocityAtTime_ReturnsNonZero()
    {
        var waypoints = new[]
        {
            new Waypoint(new Vector3D(0, 0, 0), 0),
            new Waypoint(new Vector3D(100, 0, 0), 10)
        };
        var path = FlightPath.CreateLinear(waypoints);
        
        var velocity = path.GetVelocityAtTime(5);
        Assert.True(velocity.Magnitude > 0);
    }
}

public class FlyingObjectTests
{
    [Fact]
    public void Constructor_SetsInitialState()
    {
        var path = FlightPath.CreateLinear(new[]
        {
            new Waypoint(new Vector3D(0, 0, 100), 0),
            new Waypoint(new Vector3D(100, 0, 100), 10)
        });
        var drone = new FlyingObject("test", FlyingObjectType.Drone, path);
        
        Assert.Equal("test", drone.Id);
        Assert.Equal(FlyingObjectType.Drone, drone.Type);
        Assert.False(drone.IsPlaying);
    }

    [Fact]
    public void Update_MovesAlongPath()
    {
        var path = FlightPath.CreateLinear(new[]
        {
            new Waypoint(new Vector3D(0, 0, 0), 0),
            new Waypoint(new Vector3D(100, 0, 0), 10)
        });
        var drone = new FlyingObject("test", FlyingObjectType.Drone, path);
        
        drone.Play();
        drone.Update(5);
        
        Assert.True(drone.Position.X > 0);
    }

    [Fact]
    public void Stop_ResetsPosition()
    {
        var path = FlightPath.CreateLinear(new[]
        {
            new Waypoint(new Vector3D(0, 0, 0), 0),
            new Waypoint(new Vector3D(100, 0, 0), 10)
        });
        var drone = new FlyingObject("test", FlyingObjectType.Drone, path);
        
        drone.Play();
        drone.Update(5);
        drone.Stop();
        
        Assert.Equal(0, drone.CurrentTime);
    }
}

public class CoordinateTransformerTests
{
    private readonly CoordinateTransformer _transformer = new();

    [Fact]
    public void GeoToCartesian_ConvertsCorrectly()
    {
        var geo = new GeoCoordinate(0, 0, 0);
        var ecef = _transformer.GeoToCartesian(geo);
        
        Assert.Equal(CoordinateTransformer.SemiMajorAxis, ecef.X, 0);
        Assert.Equal(0, ecef.Y, 6);
        Assert.Equal(0, ecef.Z, 6);
    }

    [Fact]
    public void RoundTrip_PreservesCoordinates()
    {
        var original = new GeoCoordinate(40.7128, -74.0060, 100);
        var ecef = _transformer.GeoToCartesian(original);
        var back = _transformer.CartesianToGeo(ecef);
        
        Assert.Equal(original.Latitude, back.Latitude, 4);
        Assert.Equal(original.Longitude, back.Longitude, 4);
        Assert.Equal(original.Altitude, back.Altitude, 0);
    }

    [Fact]
    public void WorldToLocal_TransformsCorrectly()
    {
        var world = new Vector3D(110, 110, 10);
        var origin = new Vector3D(100, 100, 0);
        var local = _transformer.WorldToLocal(world, origin, Vector3D.Zero);
        
        Assert.Equal(10, local.X, 6);
        Assert.Equal(10, local.Y, 6);
        Assert.Equal(10, local.Z, 6);
    }
}

public class DistanceCalculatorTests
{
    private readonly DistanceCalculator _calculator = new();

    [Fact]
    public void HaversineDistance_CalculatesCorrectly()
    {
        var nyc = new GeoCoordinate(40.7128, -74.0060);
        var la = new GeoCoordinate(34.0522, -118.2437);
        
        var distance = _calculator.HaversineDistance(nyc, la);
        var expectedKm = 3940; // approximately
        
        Assert.InRange(distance / 1000, expectedKm - 100, expectedKm + 100);
    }

    [Fact]
    public void VincentyDistance_CalculatesCorrectly()
    {
        var nyc = new GeoCoordinate(40.7128, -74.0060);
        var la = new GeoCoordinate(34.0522, -118.2437);
        
        var distance = _calculator.VincentyDistance(nyc, la);
        var expectedKm = 3940;
        
        Assert.InRange(distance / 1000, expectedKm - 100, expectedKm + 100);
    }

    [Fact]
    public void InitialBearing_CalculatesCorrectly()
    {
        var a = new GeoCoordinate(0, 0);
        var b = new GeoCoordinate(0, 90);
        
        var bearing = _calculator.InitialBearing(a, b);
        Assert.Equal(Math.PI / 2, bearing, 2); // East
    }
}

public class SpatialQueryTests
{
    private readonly SpatialQuery _query = new();

    [Fact]
    public void PointInPolygon_WorksCorrectly()
    {
        var square = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        };
        
        Assert.True(_query.PointInPolygon(new Vector3D(5, 5, 0), square));
        Assert.False(_query.PointInPolygon(new Vector3D(15, 5, 0), square));
    }

    [Fact]
    public void DistanceToPolygonEdge_CalculatesCorrectly()
    {
        var square = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        };
        
        var distance = _query.DistanceToPolygonEdge(new Vector3D(5, 15, 0), square);
        Assert.Equal(5, distance, 6);
    }
}

public class PolygonValidatorTests
{
    private readonly PolygonValidator _validator = new();

    [Fact]
    public void Validate_AcceptsValidPolygon()
    {
        var vertices = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0),
            new Vector3D(10, 10, 0),
            new Vector3D(0, 10, 0)
        };
        
        var result = _validator.Validate(vertices);
        Assert.True(result.IsValid);
        Assert.True(result.IsConvex);
        Assert.False(result.HasSelfIntersection);
    }

    [Fact]
    public void Validate_RejectsTooFewVertices()
    {
        var vertices = new[]
        {
            new Vector3D(0, 0, 0),
            new Vector3D(10, 0, 0)
        };
        
        var result = _validator.Validate(vertices);
        Assert.False(result.IsValid);
    }
}

public class MeshExporterTests
{
    private readonly MeshExporter _exporter = new();

    [Fact]
    public void ExportToObj_ProducesValidOutput()
    {
        var pyramid = Pyramid.CreateSquarePyramid(10, 15);
        var obj = _exporter.ExportToObj(pyramid);
        
        Assert.Contains("v ", obj);
        Assert.Contains("f ", obj);
    }

    [Fact]
    public void ExportToStl_ProducesValidOutput()
    {
        var pyramid = Pyramid.CreateSquarePyramid(10, 15);
        var stl = _exporter.ExportToStl(pyramid);
        
        Assert.Contains("solid", stl);
        Assert.Contains("facet normal", stl);
        Assert.Contains("endsolid", stl);
    }

    [Fact]
    public void ExportToGeoJson_ProducesValidJson()
    {
        var pyramid = Pyramid.CreateSquarePyramid(10, 15);
        var json = _exporter.ExportToGeoJson(pyramid);
        
        Assert.Contains("\"type\": \"Feature\"", json);
        Assert.Contains("\"geometry\"", json);
        Assert.Contains("\"coordinates\"", json);
    }
}

public class SceneTests
{
    [Fact]
    public void AddGeometry_AddsToCollection()
    {
        var scene = new Scene();
        var pyramid = Pyramid.CreateSquarePyramid(10, 15);
        
        scene.AddGeometry(pyramid);
        
        Assert.Single(scene.Geometries);
    }

    [Fact]
    public void GetSceneBounds_CalculatesCorrectly()
    {
        var scene = new Scene();
        scene.AddGeometry(Pyramid.CreateSquarePyramid(10, 15));
        scene.AddGeometry(Pyramid.CreateRegular(4, 10, 20, new Vector3D(50, 50, 0)));
        
        var bounds = scene.GetSceneBounds();
        
        Assert.True(bounds.Max.X > bounds.Min.X);
    }
}

public class PolygonBuilderTests
{
    [Fact]
    public void Build_CreatesPolygon()
    {
        var poly = new PolygonBuilder()
            .AddVertex(0, 0)
            .AddVertex(10, 0)
            .AddVertex(10, 10)
            .AddVertex(0, 10)
            .Build();
        
        Assert.Equal(4, poly.VertexCount);
    }

    [Fact]
    public void BuildExtruded_Creates3DGeometry()
    {
        var prism = new PolygonBuilder()
            .AddVertex(0, 0)
            .AddVertex(10, 0)
            .AddVertex(10, 10)
            .AddVertex(0, 10)
            .BuildExtruded(20);
        
        Assert.True(prism.Volume > 0);
    }

    [Fact]
    public void BuildPyramid_CreatesPyramid()
    {
        var pyramid = new PolygonBuilder()
            .AddVertex(0, 0)
            .AddVertex(10, 0)
            .AddVertex(10, 10)
            .AddVertex(0, 10)
            .BuildPyramid(15);
        
        Assert.True(pyramid.Volume > 0);
    }
}

public class FlightPathBuilderTests
{
    [Fact]
    public void Build_CreatesPath()
    {
        var path = new FlightPathBuilder()
            .AddWaypoint(new Vector3D(0, 0, 100), 0)
            .AddWaypoint(new Vector3D(100, 0, 100), 10)
            .Linear()
            .Build();
        
        Assert.Equal(2, path.Waypoints.Count);
    }

    [Fact]
    public void Smooth_CreatesCatmullRomPath()
    {
        var path = new FlightPathBuilder()
            .AddWaypoint(new Vector3D(0, 0, 100), 0)
            .AddWaypoint(new Vector3D(100, 0, 100), 10)
            .AddWaypoint(new Vector3D(100, 100, 100), 20)
            .Smooth()
            .Build();
        
        Assert.Equal(InterpolationType.CatmullRom, path.InterpolationType);
    }
}
