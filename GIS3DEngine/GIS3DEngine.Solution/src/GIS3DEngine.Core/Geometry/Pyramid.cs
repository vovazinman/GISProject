using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;

namespace GIS3DEngine.Core.Geometry;

/// <summary>
/// Pyramidal structure with polygon base and apex point.
/// </summary>
public class Pyramid : IGeometry3D, ITriangulatable, IHasNormals, ITransformable<Pyramid>
{
    private readonly Polygon2D _basePolygon;
    private readonly Vector3D _apex;
    private readonly Vector3D _position;
    private readonly Vector3D _rotation;
    private readonly double _scale;
    private readonly bool _includeBaseCap;

    private readonly List<Vector3D> _vertices;
    private readonly List<Triangle> _triangles;
    private readonly List<Vector3D> _faceNormals;

    private BoundingBox? _bounds;
    private Vector3D? _centroid;
    private double? _volume;
    private double? _surfaceArea;

    public Polygon2D BasePolygon => _basePolygon;
    public Vector3D Apex => _apex;
    public Vector3D Position => _position;
    public Vector3D Rotation => _rotation;
    public double Scale => _scale;
    public bool IncludeBaseCap => _includeBaseCap;

    public IReadOnlyList<Vector3D> Vertices => _vertices.AsReadOnly();
    public IReadOnlyList<Triangle> Triangles => _triangles.AsReadOnly();
    public IReadOnlyList<Vector3D> FaceNormals => _faceNormals.AsReadOnly();

    public double Height => Math.Abs(_apex.Z - _basePolygon.Centroid.Z);
    public int LateralFaceCount => _basePolygon.VertexCount;

    private Pyramid(Polygon2D basePolygon, Vector3D apex, Vector3D position, Vector3D rotation, double scale, bool includeBaseCap)
    {
        _basePolygon = basePolygon;
        _apex = apex;
        _position = position;
        _rotation = rotation;
        _scale = scale;
        _includeBaseCap = includeBaseCap;

        _vertices = new List<Vector3D>();
        _triangles = new List<Triangle>();
        _faceNormals = new List<Vector3D>();

        GenerateGeometry();
    }

    /// <summary>
    /// Creates a pyramid with apex centered above the base.
    /// </summary>
    public static Pyramid Create(Polygon2D basePolygon, double height, bool includeBaseCap = true)
    {
        var centroid = basePolygon.Centroid;
        var apex = new Vector3D(centroid.X, centroid.Y, centroid.Z + height);
        return new Pyramid(basePolygon, apex, Vector3D.Zero, Vector3D.Zero, 1.0, includeBaseCap);
    }

    /// <summary>
    /// Creates a pyramid with a custom apex position.
    /// </summary>
    public static Pyramid CreateWithApex(Polygon2D basePolygon, Vector3D apex, bool includeBaseCap = true) =>
        new(basePolygon, apex, Vector3D.Zero, Vector3D.Zero, 1.0, includeBaseCap);

    /// <summary>
    /// Creates a regular n-sided pyramid.
    /// </summary>
    public static Pyramid CreateRegular(int sides, double radius, double height, Vector3D position = default, bool includeBaseCap = true)
    {
        var basePolygon = Polygon2D.CreateRegular(sides, radius, position);
        var apex = new Vector3D(position.X, position.Y, position.Z + height);
        return new Pyramid(basePolygon, apex, Vector3D.Zero, Vector3D.Zero, 1.0, includeBaseCap);
    }

    /// <summary>
    /// Creates a tetrahedron (triangular pyramid).
    /// </summary>
    public static Pyramid CreateTetrahedron(double sideLength, Vector3D position = default)
    {
        var radius = sideLength / Math.Sqrt(3);
        var height = sideLength * Math.Sqrt(2.0 / 3.0);
        return CreateRegular(3, radius, height, position);
    }

    /// <summary>
    /// Creates a square pyramid.
    /// </summary>
    public static Pyramid CreateSquarePyramid(double sideLength, double height, Vector3D position = default)
    {
        var halfSide = sideLength / 2;
        var vertices = new[]
        {
            new Vector3D(position.X - halfSide, position.Y - halfSide, position.Z),
            new Vector3D(position.X + halfSide, position.Y - halfSide, position.Z),
            new Vector3D(position.X + halfSide, position.Y + halfSide, position.Z),
            new Vector3D(position.X - halfSide, position.Y + halfSide, position.Z)
        };
        var basePolygon = Polygon2D.FromVertices(vertices);
        var apex = new Vector3D(position.X, position.Y, position.Z + height);
        return new Pyramid(basePolygon, apex, Vector3D.Zero, Vector3D.Zero, 1.0, true);
    }

    /// <summary>
    /// Gets the slant height (distance from base edge midpoint to apex).
    /// </summary>
    public double SlantHeight
    {
        get
        {
            var baseCentroid = _basePolygon.Centroid;
            var edge = _basePolygon.GetEdge(0);
            var edgeMidpoint = (edge.Start + edge.End) * 0.5;

            var apexProjected = new Vector3D(_apex.X, _apex.Y, baseCentroid.Z);
            var baseDistance = Vector3D.Distance(edgeMidpoint, apexProjected);

            return Math.Sqrt(baseDistance * baseDistance + Height * Height);
        }
    }

    /// <summary>
    /// Checks if the pyramid is regular (apex centered above base).
    /// </summary>
    public bool IsRegular
    {
        get
        {
            var baseCentroid = _basePolygon.Centroid;
            var apexProjected = new Vector3D(_apex.X, _apex.Y, baseCentroid.Z);
            return Vector3D.Distance(baseCentroid, apexProjected) < 1e-6;
        }
    }

    public BoundingBox Bounds
    {
        get
        {
            _bounds ??= BoundingBox.FromPoints(_vertices);
            return _bounds.Value;
        }
    }

    public Vector3D Centroid
    {
        get
        {
            if (!_centroid.HasValue)
            {
                // Centroid of a pyramid is 1/4 of the way from base to apex
                var baseCentroid = _basePolygon.Centroid;
                _centroid = baseCentroid + (_apex - baseCentroid) * 0.25;
            }
            return _centroid.Value;
        }
    }

    public double Volume
    {
        get
        {
            _volume ??= _basePolygon.Area * Height / 3.0;
            return _volume.Value;
        }
    }

    public double SurfaceArea
    {
        get
        {
            _surfaceArea ??= CalculateSurfaceArea();
            return _surfaceArea.Value;
        }
    }

    /// <summary>
    /// Gets the lateral (side) surface area.
    /// </summary>
    public double LateralSurfaceArea
    {
        get
        {
            double area = 0;
            var n = _basePolygon.VertexCount;
            for (int i = 0; i < n; i++)
            {
                area += _triangles[i].Area;
            }
            return area;
        }
    }

    /// <summary>
    /// Tests if a point is inside the pyramid.
    /// </summary>
    public bool ContainsPoint(Vector3D point)
    {
        // Check if point is below apex and above base
        var basePlane = _basePolygon.BasePlane;
        var baseHeight = basePlane.SignedDistance(point);
        var apexHeight = basePlane.SignedDistance(_apex);

        if (baseHeight < 0 || baseHeight > apexHeight)
            return false;

        // Check if point is inside all lateral face half-spaces
        foreach (var normal in _faceNormals.Take(LateralFaceCount))
        {
            var facePoint = _basePolygon.Vertices[0]; // Any point on the face
            if (Vector3D.Dot(point - facePoint, normal) > 0)
                return false;
        }

        return true;
    }

    public IReadOnlyList<Triangle> Triangulate() => _triangles;

    public Vector3D GetFaceNormal(int faceIndex) =>
        faceIndex >= 0 && faceIndex < _faceNormals.Count
            ? _faceNormals[faceIndex]
            : Vector3D.UnitZ;

    /// <summary>
    /// Gets all base edges.
    /// </summary>
    public IReadOnlyList<(Vector3D Start, Vector3D End)> GetBaseEdges()
    {
        var edges = new List<(Vector3D, Vector3D)>();
        var n = _basePolygon.VertexCount;
        for (int i = 0; i < n; i++)
        {
            edges.Add((_vertices[i], _vertices[(i + 1) % n]));
        }
        return edges;
    }

    /// <summary>
    /// Gets all lateral edges (from base to apex).
    /// </summary>
    public IReadOnlyList<(Vector3D Start, Vector3D End)> GetLateralEdges()
    {
        var edges = new List<(Vector3D, Vector3D)>();
        var apexVertex = _vertices[^1];
        for (int i = 0; i < _basePolygon.VertexCount; i++)
        {
            edges.Add((_vertices[i], apexVertex));
        }
        return edges;
    }

    /// <summary>
    /// Truncates the pyramid at a relative height to create a frustum.
    /// </summary>
    public Polygon3D Truncate(double relativeHeight)
    {
        if (relativeHeight <= 0 || relativeHeight >= 1)
            throw new ArgumentOutOfRangeException(nameof(relativeHeight), "Must be between 0 and 1");

        var topScale = 1.0 - relativeHeight;
        return _basePolygon.Extrude(new ExtrusionOptions
        {
            Height = Height * relativeHeight,
            TopScale = topScale,
            Position = _position,
            Rotation = _rotation,
            CapTop = true,
            CapBottom = true
        });
    }

    public Pyramid Translate(Vector3D offset) =>
        new(_basePolygon.Translate(offset), _apex + offset, _position, _rotation, _scale, _includeBaseCap);

    public Pyramid ScaleUnitForm(double factor)
    {
        var baseCentroid = _basePolygon.Centroid;
        var scaledBase = _basePolygon.Scale(factor);
        var scaledApex = baseCentroid + (_apex - baseCentroid) * factor;
        return new Pyramid(scaledBase, scaledApex, _position, _rotation, _scale * factor, _includeBaseCap);
    }

    public Pyramid ScaleNonUnitForm(Vector3D factors)
    {
        var avgScale = (factors.X + factors.Y) / 2;
        var scaledBase = _basePolygon.Scale(avgScale);
        var baseCentroid = _basePolygon.Centroid;
        var apexOffset = _apex - baseCentroid;
        var scaledApex = baseCentroid + new Vector3D(
            apexOffset.X * factors.X,
            apexOffset.Y * factors.Y,
            apexOffset.Z * factors.Z
        );
        return new Pyramid(scaledBase, scaledApex, _position, _rotation, _scale, _includeBaseCap);
    }

    public Pyramid RotateX(double angle) =>
        new(_basePolygon, _apex, _position, _rotation + new Vector3D(angle, 0, 0), _scale, _includeBaseCap);

    public Pyramid RotateY(double angle) =>
        new(_basePolygon, _apex, _position, _rotation + new Vector3D(0, angle, 0), _scale, _includeBaseCap);

    public Pyramid RotateZ(double angle) =>
        new(_basePolygon, _apex, _position, _rotation + new Vector3D(0, 0, angle), _scale, _includeBaseCap);

    private void GenerateGeometry()
    {
        var baseVertices = _basePolygon.Vertices;
        var n = baseVertices.Count;

        // Apply transformations to base vertices
        var rotX = Matrix3x3.RotationX(_rotation.X);
        var rotY = Matrix3x3.RotationY(_rotation.Y);
        var rotZ = Matrix3x3.RotationZ(_rotation.Z);
        var rotationMatrix = rotZ * rotY * rotX;

        var baseCentroid = _basePolygon.Centroid;

        // Add transformed base vertices
        foreach (var v in baseVertices)
        {
            var local = (v - baseCentroid) * _scale;
            var rotated = rotationMatrix.Transform(local);
            _vertices.Add(rotated + baseCentroid + _position);
        }

        // Add transformed apex
        var apexLocal = (_apex - baseCentroid) * _scale;
        var apexRotated = rotationMatrix.Transform(apexLocal);
        _vertices.Add(apexRotated + baseCentroid + _position);

        var apexVertex = _vertices[^1];

        // Generate lateral face triangles
        for (int i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var tri = new Triangle(_vertices[i], _vertices[j], apexVertex);
            _triangles.Add(tri);
            _faceNormals.Add(tri.Normal);
        }

        // Generate base cap triangles
        if (_includeBaseCap)
        {
            var baseTris = _basePolygon.Triangulate();
            foreach (var tri in baseTris)
            {
                var i0 = FindVertexIndex(tri.V0, baseVertices);
                var i1 = FindVertexIndex(tri.V1, baseVertices);
                var i2 = FindVertexIndex(tri.V2, baseVertices);

                if (i0 >= 0 && i1 >= 0 && i2 >= 0)
                {
                    // Reverse winding for bottom face
                    _triangles.Add(new Triangle(_vertices[i2], _vertices[i1], _vertices[i0]));
                }
            }
            _faceNormals.Add(-rotationMatrix.Transform(Vector3D.UnitZ));
        }
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

    private double CalculateSurfaceArea()
    {
        double area = LateralSurfaceArea;
        if (_includeBaseCap)
            area += _basePolygon.Area;
        return area;
    }
}
