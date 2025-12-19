using GIS3DEngine.Core.Primitives;

namespace GIS3DEngine.Core.Interfaces;

/// <summary>
/// Winding order of polygon vertices.
/// </summary>
public enum WindingOrder
{
    CounterClockwise,
    Clockwise,
    Degenerate
}

/// <summary>
/// Type of interpolation for animation paths.
/// </summary>
public enum InterpolationType
{
    Linear,
    CatmullRom,
    Bezier
}

/// <summary>
/// Type of flying object for visualization.
/// </summary>
public enum FlyingObjectType
{
    Drone,
    Aircraft,
    Helicopter,
    Satellite,
    PointOfInterest,
    Custom
}

/// <summary>
/// Type of waypoint for path definition.
/// </summary>
public enum WaypointType
{
    Sharp,
    Smooth,
    Stop
}

/// <summary>
/// Result of polygon validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public bool IsConvex { get; init; }
    public bool HasSelfIntersection { get; init; }
    public WindingOrder WindingOrder { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();

    public static ValidationResult Valid(bool isConvex, WindingOrder windingOrder) => new()
    {
        IsValid = true,
        IsConvex = isConvex,
        HasSelfIntersection = false,
        WindingOrder = windingOrder
    };

    public static ValidationResult Invalid(string error) => new()
    {
        IsValid = false,
        Errors = new List<string> { error }
    };
}

/// <summary>
/// Base interface for all 3D geometry objects.
/// </summary>
public interface IGeometry3D
{
    BoundingBox Bounds { get; }
    Vector3D Centroid { get; }
    IReadOnlyList<Vector3D> Vertices { get; }
    double Volume { get; }
    double SurfaceArea { get; }
}

/// <summary>
/// Interface for geometry that can be triangulated for rendering.
/// </summary>
public interface ITriangulatable
{
    IReadOnlyList<Triangle> Triangles { get; }
    IReadOnlyList<Triangle> Triangulate();
}

/// <summary>
/// Interface for geometry with face normals.
/// </summary>
public interface IHasNormals
{
    IReadOnlyList<Vector3D> FaceNormals { get; }
    Vector3D GetFaceNormal(int faceIndex);
}

/// <summary>
/// Interface for transformable geometry.
/// </summary>
public interface ITransformable<T> where T : IGeometry3D
{
    T Translate(Vector3D offset);
    T ScaleUnitForm(double factor);
    T ScaleNonUnitForm(Vector3D factors);
    T RotateX(double angle);
    T RotateY(double angle);
    T RotateZ(double angle);
}

/// <summary>
/// Interface for polygon validation operations.
/// </summary>
public interface IPolygonValidator
{
    ValidationResult Validate(IReadOnlyList<Vector3D> vertices);
    bool IsConvex(IReadOnlyList<Vector3D> vertices);
    bool IsSelfIntersecting(IReadOnlyList<Vector3D> vertices);
    WindingOrder GetWindingOrder(IReadOnlyList<Vector3D> vertices);
}

/// <summary>
/// Interface for coordinate transformation operations.
/// </summary>
public interface ICoordinateTransformer
{
    Vector3D GeoToCartesian(GeoCoordinate coordinate);
    GeoCoordinate CartesianToGeo(Vector3D position);
    Vector3D WorldToLocal(Vector3D worldPosition, Vector3D origin, Vector3D rotation);
    Vector3D LocalToWorld(Vector3D localPosition, Vector3D origin, Vector3D rotation);
}

/// <summary>
/// Interface for spatial query operations.
/// </summary>
public interface ISpatialQuery
{
    bool PointInPolygon(Vector3D point, IReadOnlyList<Vector3D> polygonVertices);
    double DistanceBetween(GeoCoordinate a, GeoCoordinate b);
    double BearingBetween(GeoCoordinate from, GeoCoordinate to);
    double HeightAbovePolygon(Vector3D point, IGeometry3D polygon);
}

/// <summary>
/// Interface for animation paths.
/// </summary>
public interface IAnimationPath
{
    double TotalDuration { get; }
    double TotalDistance { get; }
    bool IsLooping { get; }
    
    Vector3D GetPositionAtTime(double time);
    Vector3D GetVelocityAtTime(double time);
    Vector3D GetDirectionAtTime(double time);
}

/// <summary>
/// Interface for interpolation strategies.
/// </summary>
public interface IInterpolator
{
    Vector3D Interpolate(Vector3D start, Vector3D end, double t);
    double Interpolate(double start, double end, double t);
}

/// <summary>
/// Interface for animated flying objects.
/// </summary>
public interface IFlyingObject
{
    string Id { get; }
    string Name { get; }
    FlyingObjectType Type { get; }
    Vector3D Position { get; }
    Vector3D Velocity { get; }
    double Heading { get; }
    double Pitch { get; }
    bool IsPlaying { get; }
    
    void Update(double deltaTime);
    void Play();
    void Pause();
    void Stop();
}

/// <summary>
/// Interface for mesh export operations.
/// </summary>
public interface IMeshExporter
{
    string ExportToObj(IGeometry3D geometry, bool includeNormals = true);
    string ExportToStl(IGeometry3D geometry);
    string ExportToGeoJson(IGeometry3D geometry, ICoordinateTransformer? transformer = null);
}
