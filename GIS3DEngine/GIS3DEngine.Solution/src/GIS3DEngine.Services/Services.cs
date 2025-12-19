using System.Text;
using System.Text.Json;
using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;
using GIS3DEngine.Core.Geometry;
using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Spatial;

namespace GIS3DEngine.Services;

/// <summary>
/// Factory for creating geometric objects.
/// </summary>
public class GeometryFactory
{
    private readonly CoordinateTransformer _transformer;
    private readonly PolygonValidator _validator;

    public GeometryFactory()
    {
        _transformer = new CoordinateTransformer();
        _validator = new PolygonValidator();
    }

    public CoordinateTransformer Transformer => _transformer;
    public PolygonValidator Validator => _validator;

    public Polygon2D CreatePolygon(IEnumerable<Vector3D> vertices) =>
        Polygon2D.FromVertices(vertices);

    public Polygon2D CreateRegularPolygon(int sides, double radius, Vector3D center = default) =>
        Polygon2D.CreateRegular(sides, radius, center);

    public Polygon3D ExtrudePolygon(Polygon2D polygon, double height, double topScale = 1.0) =>
        polygon.Extrude(new ExtrusionOptions { Height = height, TopScale = topScale });

    public Pyramid CreatePyramid(Polygon2D basePolygon, double height) =>
        Pyramid.Create(basePolygon, height);

    public Pyramid CreateRegularPyramid(int sides, double radius, double height, Vector3D position = default) =>
        Pyramid.CreateRegular(sides, radius, height, position);

    public ValidationResult ValidatePolygon(IReadOnlyList<Vector3D> vertices) =>
        _validator.Validate(vertices);
}

/// <summary>
/// Fluent builder for constructing polygons.
/// </summary>
public class PolygonBuilder
{
    private readonly List<Vector3D> _vertices = new();
    private readonly List<GeoCoordinate> _geoCoordinates = new();
    private readonly CoordinateTransformer _transformer = new();

    public PolygonBuilder AddVertex(double x, double y, double z = 0)
    {
        _vertices.Add(new Vector3D(x, y, z));
        return this;
    }

    public PolygonBuilder AddVertex(Vector3D vertex)
    {
        _vertices.Add(vertex);
        return this;
    }

    public PolygonBuilder AddVertices(IEnumerable<Vector3D> vertices)
    {
        _vertices.AddRange(vertices);
        return this;
    }

    public PolygonBuilder AddGeoCoordinate(double latitude, double longitude, double altitude = 0)
    {
        _geoCoordinates.Add(new GeoCoordinate(latitude, longitude, altitude));
        return this;
    }

    public PolygonBuilder AddGeoCoordinates(IEnumerable<GeoCoordinate> coordinates)
    {
        _geoCoordinates.AddRange(coordinates);
        return this;
    }

    public PolygonBuilder EnsureCounterClockwise()
    {
        var polygon = BuildInternal();
        if (polygon.WindingOrder == WindingOrder.Clockwise)
        {
            _vertices.Clear();
            _vertices.AddRange(polygon.Vertices.Reverse());
        }
        return this;
    }

    public Polygon2D Build()
    {
        return BuildInternal();
    }

    public Polygon3D BuildExtruded(double height, double topScale = 1.0)
    {
        return BuildInternal().Extrude(new ExtrusionOptions
        {
            Height = height,
            TopScale = topScale
        });
    }

    public Pyramid BuildPyramid(double height)
    {
        return Pyramid.Create(BuildInternal(), height);
    }

    private Polygon2D BuildInternal()
    {
        if (_geoCoordinates.Count > 0)
        {
            // Convert geo coordinates to local vertices
            if (_geoCoordinates.Count >= 3)
            {
                var reference = _geoCoordinates[0];
                var localVertices = _geoCoordinates
                    .Select(c => _transformer.GeoToENU(c, reference))
                    .ToList();
                return Polygon2D.FromVertices(localVertices);
            }
        }

        return Polygon2D.FromVertices(_vertices);
    }
}

/// <summary>
/// Fluent builder for constructing pyramids.
/// </summary>
public class PyramidBuilder
{
    private Polygon2D? _basePolygon;
    private double _height = 10;
    private Vector3D? _apex;
    private Vector3D _position = Vector3D.Zero;
    private Vector3D _rotation = Vector3D.Zero;
    private double _scale = 1.0;
    private bool _includeBaseCap = true;

    public PyramidBuilder WithBase(Polygon2D basePolygon)
    {
        _basePolygon = basePolygon;
        return this;
    }

    public PyramidBuilder WithRegularBase(int sides, double radius)
    {
        _basePolygon = Polygon2D.CreateRegular(sides, radius);
        return this;
    }

    public PyramidBuilder WithSquareBase(double sideLength)
    {
        var half = sideLength / 2;
        _basePolygon = Polygon2D.FromVertices(new[]
        {
            new Vector3D(-half, -half, 0),
            new Vector3D(half, -half, 0),
            new Vector3D(half, half, 0),
            new Vector3D(-half, half, 0)
        });
        return this;
    }

    public PyramidBuilder WithHeight(double height)
    {
        _height = height;
        return this;
    }

    public PyramidBuilder WithApex(Vector3D apex)
    {
        _apex = apex;
        return this;
    }

    public PyramidBuilder AtPosition(Vector3D position)
    {
        _position = position;
        return this;
    }

    public PyramidBuilder WithRotation(Vector3D rotation)
    {
        _rotation = rotation;
        return this;
    }

    public PyramidBuilder WithScale(double scale)
    {
        _scale = scale;
        return this;
    }

    public PyramidBuilder WithBaseCap(bool include = true)
    {
        _includeBaseCap = include;
        return this;
    }

    public Pyramid Build()
    {
        if (_basePolygon == null)
            throw new InvalidOperationException("Base polygon must be specified");

        var translatedBase = _basePolygon.Translate(_position);

        if (_apex.HasValue)
        {
            return Pyramid.CreateWithApex(translatedBase, _apex.Value, _includeBaseCap);
        }

        return Pyramid.Create(translatedBase, _height, _includeBaseCap);
    }
}

/// <summary>
/// Fluent builder for constructing flight paths.
/// </summary>
public class FlightPathBuilder
{
    private readonly List<Waypoint> _waypoints = new();
    private InterpolationType _interpolationType = InterpolationType.Linear;
    private bool _isLooping = false;
    private double _defaultSpeed = 10.0;
    private readonly CoordinateTransformer _transformer = new();

    public FlightPathBuilder AddWaypoint(Vector3D position, double time, WaypointType type = WaypointType.Smooth)
    {
        _waypoints.Add(new Waypoint(position, time, null, type));
        return this;
    }

    public FlightPathBuilder AddWaypointAtSpeed(Vector3D position, double speed)
    {
        double time = 0;
        if (_waypoints.Count > 0)
        {
            var lastWaypoint = _waypoints[^1];
            var distance = Vector3D.Distance(lastWaypoint.Position, position);
            time = lastWaypoint.Time + distance / speed;
        }
        _waypoints.Add(new Waypoint(position, time, speed));
        return this;
    }

    public FlightPathBuilder AddPositions(IEnumerable<Vector3D> positions)
    {
        foreach (var pos in positions)
        {
            AddWaypointAtSpeed(pos, _defaultSpeed);
        }
        return this;
    }

    public FlightPathBuilder AddGeoWaypoint(GeoCoordinate coordinate, double time, GeoCoordinate? reference = null)
    {
        var refCoord = reference ?? (
            _waypoints.Count > 0
                ? new GeoCoordinate(0, 0, 0)
                : coordinate
        );
        var localPos = _transformer.GeoToENU(coordinate, refCoord);
        _waypoints.Add(new Waypoint(localPos, time));
        return this;
    }

    public FlightPathBuilder WithInterpolation(InterpolationType type)
    {
        _interpolationType = type;
        return this;
    }

    public FlightPathBuilder Linear()
    {
        _interpolationType = InterpolationType.Linear;
        return this;
    }

    public FlightPathBuilder Smooth()
    {
        _interpolationType = InterpolationType.CatmullRom;
        return this;
    }

    public FlightPathBuilder Looping(bool loop = true)
    {
        _isLooping = loop;
        return this;
    }

    public FlightPathBuilder WithDefaultSpeed(double speed)
    {
        _defaultSpeed = speed;
        return this;
    }

    public FlightPath Build()
    {
        return _interpolationType switch
        {
            InterpolationType.CatmullRom => FlightPath.CreateSpline(_waypoints, _isLooping),
            _ => FlightPath.CreateLinear(_waypoints, _isLooping)
        };
    }

    /// <summary>
    /// Creates a tour path visiting multiple geometries.
    /// </summary>
    public static FlightPath CreateTour(IEnumerable<IGeometry3D> geometries, double altitude, double speed)
    {
        var positions = geometries
            .Select(g => g.Centroid.WithZ(g.Bounds.Max.Z + altitude))
            .ToList();

        return FlightPath.CreateWithSpeed(positions, speed, InterpolationType.CatmullRom);
    }
}

/// <summary>
/// Exports geometry to various file formats.
/// </summary>
public class MeshExporter : IMeshExporter
{
    /// <summary>
    /// Exports geometry to Wavefront OBJ format.
    /// </summary>
    public string ExportToObj(IGeometry3D geometry, bool includeNormals = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# GIS3DEngine OBJ Export");
        sb.AppendLine($"# Vertices: {geometry.Vertices.Count}");
        sb.AppendLine();

        // Vertices
        foreach (var v in geometry.Vertices)
        {
            sb.AppendLine($"v {v.X:F6} {v.Y:F6} {v.Z:F6}");
        }
        sb.AppendLine();

        // Normals
        if (includeNormals && geometry is IHasNormals hasNormals)
        {
            foreach (var n in hasNormals.FaceNormals)
            {
                sb.AppendLine($"vn {n.X:F6} {n.Y:F6} {n.Z:F6}");
            }
            sb.AppendLine();
        }

        // Faces
        if (geometry is ITriangulatable triangulatable)
        {
            var triangles = triangulatable.Triangles;
            var vertices = geometry.Vertices;

            foreach (var tri in triangles)
            {
                var i0 = FindVertexIndex(tri.V0, vertices) + 1;
                var i1 = FindVertexIndex(tri.V1, vertices) + 1;
                var i2 = FindVertexIndex(tri.V2, vertices) + 1;

                if (i0 > 0 && i1 > 0 && i2 > 0)
                {
                    sb.AppendLine($"f {i0} {i1} {i2}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports geometry to STL format.
    /// </summary>
    public string ExportToStl(IGeometry3D geometry)
    {
        var sb = new StringBuilder();
        sb.AppendLine("solid GIS3DEngine");

        if (geometry is ITriangulatable triangulatable)
        {
            foreach (var tri in triangulatable.Triangles)
            {
                var normal = tri.Normal;
                sb.AppendLine($"  facet normal {normal.X:E} {normal.Y:E} {normal.Z:E}");
                sb.AppendLine("    outer loop");
                sb.AppendLine($"      vertex {tri.V0.X:E} {tri.V0.Y:E} {tri.V0.Z:E}");
                sb.AppendLine($"      vertex {tri.V1.X:E} {tri.V1.Y:E} {tri.V1.Z:E}");
                sb.AppendLine($"      vertex {tri.V2.X:E} {tri.V2.Y:E} {tri.V2.Z:E}");
                sb.AppendLine("    endloop");
                sb.AppendLine("  endfacet");
            }
        }

        sb.AppendLine("endsolid GIS3DEngine");
        return sb.ToString();
    }

    /// <summary>
    /// Exports geometry to GeoJSON format.
    /// </summary>
    public string ExportToGeoJson(IGeometry3D geometry, ICoordinateTransformer? transformer = null)
    {
        var coordinates = new List<List<double[]>>();
        var ring = new List<double[]>();

        foreach (var v in geometry.Vertices)
        {
            if (transformer != null)
            {
                var geo = ((CoordinateTransformer)transformer).CartesianToGeo(v);
                ring.Add(new[] { geo.Longitude, geo.Latitude, geo.Altitude });
            }
            else
            {
                ring.Add(new[] { v.X, v.Y, v.Z });
            }
        }

        // Close the ring
        if (ring.Count > 0)
        {
            ring.Add(ring[0]);
        }
        coordinates.Add(ring);

        var feature = new
        {
            type = "Feature",
            geometry = new
            {
                type = "Polygon",
                coordinates
            },
            properties = new
            {
                volume = geometry.Volume,
                surfaceArea = geometry.SurfaceArea,
                centroid = new[] { geometry.Centroid.X, geometry.Centroid.Y, geometry.Centroid.Z }
            }
        };

        return JsonSerializer.Serialize(feature, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Exports flight path to GeoJSON LineString.
    /// </summary>
    public string ExportFlightPathToGeoJson(FlightPath path, int sampleCount = 100, CoordinateTransformer? transformer = null)
    {
        var coordinates = new List<double[]>();
        var samples = path.SamplePath(sampleCount);

        foreach (var pos in samples)
        {
            if (transformer != null)
            {
                var geo = transformer.CartesianToGeo(pos);
                coordinates.Add(new[] { geo.Longitude, geo.Latitude, geo.Altitude });
            }
            else
            {
                coordinates.Add(new[] { pos.X, pos.Y, pos.Z });
            }
        }

        var feature = new
        {
            type = "Feature",
            geometry = new
            {
                type = "LineString",
                coordinates
            },
            properties = new
            {
                totalDistance = path.TotalDistance,
                totalDuration = path.TotalDuration,
                waypointCount = path.Waypoints.Count,
                isLooping = path.IsLooping
            }
        };

        return JsonSerializer.Serialize(feature, new JsonSerializerOptions { WriteIndented = true });
    }

    private static int FindVertexIndex(Vector3D vertex, IReadOnlyList<Vector3D> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if (Vector3D.DistanceSquared(vertex, vertices[i]) < 1e-10)
                return i;
        }
        return -1;
    }
}

/// <summary>
/// Scene manager for geometry and animation.
/// </summary>
public class Scene
{
    private readonly List<IGeometry3D> _geometries = new();
    private readonly List<FlyingObject> _flyingObjects = new();
    private readonly TimeController _timeController = new();

    public IReadOnlyList<IGeometry3D> Geometries => _geometries.AsReadOnly();
    public IReadOnlyList<FlyingObject> FlyingObjects => _flyingObjects.AsReadOnly();
    public TimeController TimeController => _timeController;

    public void AddGeometry(IGeometry3D geometry)
    {
        _geometries.Add(geometry);
    }

    public void AddFlyingObject(FlyingObject obj)
    {
        _flyingObjects.Add(obj);
        _timeController.AddObject(obj);
    }

    public FlyingObject CreateFlyingObject(string id, FlyingObjectType type, FlightPath path)
    {
        var obj = new FlyingObject(id, type, path);
        AddFlyingObject(obj);
        return obj;
    }

    public void Update(double deltaTime)
    {
        _timeController.Update(deltaTime);
    }

    public void StartSimulation()
    {
        _timeController.Start();
    }

    public void StopSimulation()
    {
        _timeController.Stop();
    }

    public BoundingBox GetSceneBounds()
    {
        if (_geometries.Count == 0)
            return new BoundingBox(Vector3D.Zero, Vector3D.Zero);

        var bounds = _geometries[0].Bounds;
        foreach (var geom in _geometries.Skip(1))
        {
            bounds = bounds.Union(geom.Bounds);
        }
        return bounds;
    }

    public void Clear()
    {
        _geometries.Clear();
        _flyingObjects.Clear();
        _timeController.Reset();
    }
}
