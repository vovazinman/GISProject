using System.Text.Json.Serialization;

namespace GIS3DEngine.Core.Primitives;

/// <summary>
/// Immutable 3D vector with full mathematical operations.
/// </summary>
public readonly struct Vector3D : IEquatable<Vector3D>
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public Vector3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    [JsonIgnore]
    public static Vector3D Zero => new(0, 0, 0);
    [JsonIgnore]
    public static Vector3D One => new(1, 1, 1);
    [JsonIgnore]
    public static Vector3D UnitX => new(1, 0, 0);
    [JsonIgnore]
    public static Vector3D UnitY => new(0, 1, 0);
    [JsonIgnore]
    public static Vector3D UnitZ => new(0, 0, 1);

    [JsonIgnore]
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
    [JsonIgnore]
    public double MagnitudeSquared => X * X + Y * Y + Z * Z;

    [JsonIgnore]
    public Vector3D Normalized
    {
        get
        {
            var mag = Magnitude;
            return mag > 1e-10 ? new Vector3D(X / mag, Y / mag, Z / mag) : Zero;
        }
    }

    public static double Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3D Cross(Vector3D a, Vector3D b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X
    );

    public static Vector3D Lerp(Vector3D a, Vector3D b, double t) => new(
        a.X + (b.X - a.X) * t,
        a.Y + (b.Y - a.Y) * t,
        a.Z + (b.Z - a.Z) * t
    );

    public static double Distance(Vector3D a, Vector3D b) => (b - a).Magnitude;
    public static double DistanceSquared(Vector3D a, Vector3D b) => (b - a).MagnitudeSquared;

    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator *(Vector3D v, double s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3D operator *(double s, Vector3D v) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3D operator /(Vector3D v, double s) => new(v.X / s, v.Y / s, v.Z / s);
    public static Vector3D operator -(Vector3D v) => new(-v.X, -v.Y, -v.Z);

    public bool Equals(Vector3D other) =>
        Math.Abs(X - other.X) < 1e-10 &&
        Math.Abs(Y - other.Y) < 1e-10 &&
        Math.Abs(Z - other.Z) < 1e-10;

    public override bool Equals(object? obj) => obj is Vector3D other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public static bool operator ==(Vector3D left, Vector3D right) => left.Equals(right);
    public static bool operator !=(Vector3D left, Vector3D right) => !left.Equals(right);

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";

    public Vector3D WithX(double x) => new(x, Y, Z);
    public Vector3D WithY(double y) => new(X, y, Z);
    public Vector3D WithZ(double z) => new(X, Y, z);

    /// <summary>
    /// Rotates vector around X axis by given angle in radians.
    /// </summary>
    public Vector3D RotateX(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Vector3D(X, Y * cos - Z * sin, Y * sin + Z * cos);
    }

    /// <summary>
    /// Rotates vector around Y axis by given angle in radians.
    /// </summary>
    public Vector3D RotateY(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Vector3D(X * cos + Z * sin, Y, -X * sin + Z * cos);
    }

    /// <summary>
    /// Rotates vector around Z axis by given angle in radians.
    /// </summary>
    public Vector3D RotateZ(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Vector3D(X * cos - Y * sin, X * sin + Y * cos, Z);
    }

    /// <summary>
    /// Applies Euler rotation (X, then Y, then Z) in radians.
    /// </summary>
    public Vector3D Rotate(Vector3D eulerAngles) =>
        RotateX(eulerAngles.X).RotateY(eulerAngles.Y).RotateZ(eulerAngles.Z);
}

/// <summary>
/// WGS84 geographic coordinate with latitude, longitude, and altitude.
/// </summary>
public readonly struct GeoCoordinate : IEquatable<GeoCoordinate>
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double Altitude { get; }

    public GeoCoordinate(double latitude, double longitude, double altitude = 0)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees");
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees");

        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
    }

    [JsonIgnore]
    public double LatitudeRadians => Latitude * Math.PI / 180.0;
    [JsonIgnore]
    public double LongitudeRadians => Longitude * Math.PI / 180.0;

    public bool Equals(GeoCoordinate other) =>
        Math.Abs(Latitude - other.Latitude) < 1e-10 &&
        Math.Abs(Longitude - other.Longitude) < 1e-10 &&
        Math.Abs(Altitude - other.Altitude) < 1e-6;

    public override bool Equals(object? obj) => obj is GeoCoordinate other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude, Altitude);
    public static bool operator ==(GeoCoordinate left, GeoCoordinate right) => left.Equals(right);
    public static bool operator !=(GeoCoordinate left, GeoCoordinate right) => !left.Equals(right);

    public override string ToString() => $"({Latitude:F6}°, {Longitude:F6}°, {Altitude:F2}m)";

    public GeoCoordinate WithAltitude(double altitude) => new(Latitude, Longitude, altitude);
}

/// <summary>
/// 3D point supporting both local Cartesian and world geographic coordinates.
/// </summary>
public readonly struct GeoPoint : IEquatable<GeoPoint>
{
    public Vector3D LocalPosition { get; }
    public GeoCoordinate? WorldCoordinate { get; }

    public GeoPoint(Vector3D localPosition)
    {
        LocalPosition = localPosition;
        WorldCoordinate = null;
    }

    public GeoPoint(Vector3D localPosition, GeoCoordinate worldCoordinate)
    {
        LocalPosition = localPosition;
        WorldCoordinate = worldCoordinate;
    }

    public GeoPoint(double x, double y, double z) : this(new Vector3D(x, y, z)) { }

    public double X => LocalPosition.X;
    public double Y => LocalPosition.Y;
    public double Z => LocalPosition.Z;

    public bool HasWorldCoordinate => WorldCoordinate.HasValue;

    public bool Equals(GeoPoint other) => LocalPosition.Equals(other.LocalPosition);
    public override bool Equals(object? obj) => obj is GeoPoint other && Equals(other);
    public override int GetHashCode() => LocalPosition.GetHashCode();
    public static bool operator ==(GeoPoint left, GeoPoint right) => left.Equals(right);
    public static bool operator !=(GeoPoint left, GeoPoint right) => !left.Equals(right);

    public override string ToString() => WorldCoordinate.HasValue
        ? $"Local: {LocalPosition}, World: {WorldCoordinate}"
        : $"Local: {LocalPosition}";
}

/// <summary>
/// Axis-aligned bounding box for spatial queries.
/// </summary>
public readonly struct BoundingBox : IEquatable<BoundingBox>
{
    public Vector3D Min { get; }
    public Vector3D Max { get; }

    public BoundingBox(Vector3D min, Vector3D max)
    {
        Min = new Vector3D(
            Math.Min(min.X, max.X),
            Math.Min(min.Y, max.Y),
            Math.Min(min.Z, max.Z)
        );
        Max = new Vector3D(
            Math.Max(min.X, max.X),
            Math.Max(min.Y, max.Y),
            Math.Max(min.Z, max.Z)
        );
    }
    [JsonIgnore]
    public Vector3D Size => Max - Min;
    [JsonIgnore]
    public Vector3D Center => (Min + Max) * 0.5;
    [JsonIgnore]
    public double Volume => (Max.X - Min.X) * (Max.Y - Min.Y) * (Max.Z - Min.Z);

    public static BoundingBox FromPoints(IEnumerable<Vector3D> points)
    {
        var pointList = points.ToList();
        if (pointList.Count == 0)
            return new BoundingBox(Vector3D.Zero, Vector3D.Zero);

        var min = pointList[0];
        var max = pointList[0];

        foreach (var p in pointList.Skip(1))
        {
            min = new Vector3D(Math.Min(min.X, p.X), Math.Min(min.Y, p.Y), Math.Min(min.Z, p.Z));
            max = new Vector3D(Math.Max(max.X, p.X), Math.Max(max.Y, p.Y), Math.Max(max.Z, p.Z));
        }

        return new BoundingBox(min, max);
    }

    public bool Contains(Vector3D point) =>
        point.X >= Min.X && point.X <= Max.X &&
        point.Y >= Min.Y && point.Y <= Max.Y &&
        point.Z >= Min.Z && point.Z <= Max.Z;

    public bool Intersects(BoundingBox other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
        Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;

    public BoundingBox Expand(double amount) =>
        new(Min - new Vector3D(amount, amount, amount), Max + new Vector3D(amount, amount, amount));

    public BoundingBox Union(BoundingBox other) =>
        new(
            new Vector3D(Math.Min(Min.X, other.Min.X), Math.Min(Min.Y, other.Min.Y), Math.Min(Min.Z, other.Min.Z)),
            new Vector3D(Math.Max(Max.X, other.Max.X), Math.Max(Max.Y, other.Max.Y), Math.Max(Max.Z, other.Max.Z))
        );

    public bool Equals(BoundingBox other) => Min.Equals(other.Min) && Max.Equals(other.Max);
    public override bool Equals(object? obj) => obj is BoundingBox other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Min, Max);
    public static bool operator ==(BoundingBox left, BoundingBox right) => left.Equals(right);
    public static bool operator !=(BoundingBox left, BoundingBox right) => !left.Equals(right);

    public override string ToString() => $"[{Min} -> {Max}]";
}

/// <summary>
/// Mathematical plane defined by point and normal.
/// </summary>
public readonly struct Plane : IEquatable<Plane>
{
    public Vector3D Point { get; }
    public Vector3D Normal { get; }

    public Plane(Vector3D point, Vector3D normal)
    {
        Point = point;
        Normal = normal.Normalized;
    }

    public static Plane FromPoints(Vector3D a, Vector3D b, Vector3D c)
    {
        var normal = Vector3D.Cross(b - a, c - a).Normalized;
        return new Plane(a, normal);
    }

    public double SignedDistance(Vector3D point) => Vector3D.Dot(point - Point, Normal);

    public Vector3D Project(Vector3D point) => point - Normal * SignedDistance(point);

    public bool IsAbove(Vector3D point) => SignedDistance(point) > 0;
    public bool IsBelow(Vector3D point) => SignedDistance(point) < 0;
    public bool IsOn(Vector3D point, double tolerance = 1e-10) => Math.Abs(SignedDistance(point)) < tolerance;

    public bool Equals(Plane other) => Point.Equals(other.Point) && Normal.Equals(other.Normal);
    public override bool Equals(object? obj) => obj is Plane other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Point, Normal);
    public static bool operator ==(Plane left, Plane right) => left.Equals(right);
    public static bool operator !=(Plane left, Plane right) => !left.Equals(right);

    public override string ToString() => $"Plane(Point: {Point}, Normal: {Normal})";
}

/// <summary>
/// Triangle with vertices and computed properties.
/// </summary>
public readonly struct Triangle : IEquatable<Triangle>
{
    public Vector3D V0 { get; }
    public Vector3D V1 { get; }
    public Vector3D V2 { get; }

    public Triangle(Vector3D v0, Vector3D v1, Vector3D v2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
    }

    [JsonIgnore]
    public Vector3D Normal => Vector3D.Cross(V1 - V0, V2 - V0).Normalized;

    [JsonIgnore]
    public double Area
    {
        get
        {
            var cross = Vector3D.Cross(V1 - V0, V2 - V0);
            return cross.Magnitude * 0.5;
        }
    }

    [JsonIgnore]
    public Vector3D Centroid => (V0 + V1 + V2) / 3.0;

    [JsonIgnore]
    public Vector3D Edge0 => V1 - V0;
    [JsonIgnore]
    public Vector3D Edge1 => V2 - V1;
    [JsonIgnore]
    public Vector3D Edge2 => V0 - V2;

    public bool Contains(Vector3D point)
    {
        var v0v1 = V1 - V0;
        var v0v2 = V2 - V0;
        var v0p = point - V0;

        var dot00 = Vector3D.Dot(v0v2, v0v2);
        var dot01 = Vector3D.Dot(v0v2, v0v1);
        var dot02 = Vector3D.Dot(v0v2, v0p);
        var dot11 = Vector3D.Dot(v0v1, v0v1);
        var dot12 = Vector3D.Dot(v0v1, v0p);

        var invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= 0 && v >= 0 && u + v <= 1;
    }

    public bool Equals(Triangle other) => V0.Equals(other.V0) && V1.Equals(other.V1) && V2.Equals(other.V2);
    public override bool Equals(object? obj) => obj is Triangle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(V0, V1, V2);
    public static bool operator ==(Triangle left, Triangle right) => left.Equals(right);
    public static bool operator !=(Triangle left, Triangle right) => !left.Equals(right);

    public override string ToString() => $"Triangle({V0}, {V1}, {V2})";
}

/// <summary>
/// 3x3 rotation matrix.
/// </summary>
public readonly struct Matrix3x3
{
    private readonly double[,] _values;

    public Matrix3x3(double[,] values)
    {
        if (values.GetLength(0) != 3 || values.GetLength(1) != 3)
            throw new ArgumentException("Matrix must be 3x3");
        _values = (double[,])values.Clone();
    }

    public double this[int row, int col] => _values[row, col];

    public static Matrix3x3 Identity => new(new double[,] {
        { 1, 0, 0 },
        { 0, 1, 0 },
        { 0, 0, 1 }
    });

    public static Matrix3x3 RotationX(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Matrix3x3(new double[,] {
            { 1, 0, 0 },
            { 0, cos, -sin },
            { 0, sin, cos }
        });
    }

    public static Matrix3x3 RotationY(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Matrix3x3(new double[,] {
            { cos, 0, sin },
            { 0, 1, 0 },
            { -sin, 0, cos }
        });
    }

    public static Matrix3x3 RotationZ(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new Matrix3x3(new double[,] {
            { cos, -sin, 0 },
            { sin, cos, 0 },
            { 0, 0, 1 }
        });
    }

    public Vector3D Transform(Vector3D v) => new(
        _values[0, 0] * v.X + _values[0, 1] * v.Y + _values[0, 2] * v.Z,
        _values[1, 0] * v.X + _values[1, 1] * v.Y + _values[1, 2] * v.Z,
        _values[2, 0] * v.X + _values[2, 1] * v.Y + _values[2, 2] * v.Z
    );

    public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
    {
        var result = new double[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    result[i, j] += a[i, k] * b[k, j];
        return new Matrix3x3(result);
    }
}
