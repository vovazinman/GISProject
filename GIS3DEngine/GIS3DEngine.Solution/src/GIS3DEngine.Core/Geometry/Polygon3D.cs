using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;

namespace GIS3DEngine.Core.Geometry;

/// <summary>
/// 3D extruded polygon with configurable height, scale, rotation, and capping.
/// </summary>
public class Polygon3D : IGeometry3D, ITriangulatable, IHasNormals, ITransformable<Polygon3D>
{
    private readonly List<Vector3D> _vertices;
    private readonly List<Triangle> _triangles;
    private readonly List<Vector3D> _faceNormals;
    private readonly Polygon2D _basePolygon;
    private readonly ExtrusionOptions _options;

    public Polygon2D BasePolygon => _basePolygon;
    public double Height => _options.Height;
    public double TopScale => _options.TopScale;
    public Vector3D Rotation => _options.Rotation;
    public Vector3D Position => _options.Position;

    public IReadOnlyList<Vector3D> Vertices => _vertices.AsReadOnly();
    public IReadOnlyList<Triangle> Triangles => _triangles.AsReadOnly();
    public IReadOnlyList<Vector3D> FaceNormals => _faceNormals.AsReadOnly();

    private BoundingBox? _bounds;
    private Vector3D? _centroid;
    private double? _volume;
    private double? _surfaceArea;

    private Polygon3D(Polygon2D basePolygon, ExtrusionOptions options)
    {
        _basePolygon = basePolygon;
        _options = options;
        _vertices = new List<Vector3D>();
        _triangles = new List<Triangle>();
        _faceNormals = new List<Vector3D>();

        GenerateGeometry();
    }

    /// <summary>
    /// Creates a 3D extruded polygon from a 2D base.
    /// </summary>
    public static Polygon3D Extrude(Polygon2D basePolygon, ExtrusionOptions options) =>
        new(basePolygon, options);

    /// <summary>
    /// Creates a prism (uniform cross-section extrusion).
    /// </summary>
    public static Polygon3D CreatePrism(Polygon2D basePolygon, double height) =>
        new(basePolygon, new ExtrusionOptions { Height = height });

    /// <summary>
    /// Creates a tapered extrusion (frustum-like).
    /// </summary>
    public static Polygon3D CreateTapered(Polygon2D basePolygon, double height, double topScale) =>
        new(basePolygon, new ExtrusionOptions { Height = height, TopScale = topScale });

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
                var cx = _vertices.Average(v => v.X);
                var cy = _vertices.Average(v => v.Y);
                var cz = _vertices.Average(v => v.Z);
                _centroid = new Vector3D(cx, cy, cz);
            }
            return _centroid.Value;
        }
    }

    public double Volume
    {
        get
        {
            _volume ??= CalculateVolume();
            return _volume.Value;
        }
    }

    public double SurfaceArea
    {
        get
        {
            _surfaceArea ??= _triangles.Sum(t => t.Area);
            return _surfaceArea.Value;
        }
    }

    public int BottomVertexCount => _basePolygon.VertexCount;
    public int TopVertexCount => _basePolygon.VertexCount;

    /// <summary>
    /// Gets the bottom (base) vertices.
    /// </summary>
    public IReadOnlyList<Vector3D> BottomVertices =>
        _vertices.Take(BottomVertexCount).ToList();

    /// <summary>
    /// Gets the top vertices.
    /// </summary>
    public IReadOnlyList<Vector3D> TopVertices =>
        _vertices.Skip(BottomVertexCount).Take(TopVertexCount).ToList();

    public IReadOnlyList<Triangle> Triangulate() => _triangles;

    public Vector3D GetFaceNormal(int faceIndex) =>
        faceIndex >= 0 && faceIndex < _faceNormals.Count
            ? _faceNormals[faceIndex]
            : Vector3D.UnitZ;

    public Polygon3D Translate(Vector3D offset)
    {
        var newOptions = new ExtrusionOptions
        {
            Height = _options.Height,
            TopScale = _options.TopScale,
            Rotation = _options.Rotation,
            Position = _options.Position + offset,
            CapTop = _options.CapTop,
            CapBottom = _options.CapBottom
        };
        return new Polygon3D(_basePolygon, newOptions);
    }

    public Polygon3D ScaleUnitForm(double factor) => ScaleNonUnitForm(new Vector3D(factor, factor, factor));

    public Polygon3D ScaleNonUnitForm(Vector3D factors)
    {
        var scaledBase = _basePolygon.Scale(Math.Sqrt(factors.X * factors.Y));
        var newOptions = new ExtrusionOptions
        {
            Height = _options.Height * factors.Z,
            TopScale = _options.TopScale,
            Rotation = _options.Rotation,
            Position = _options.Position,
            CapTop = _options.CapTop,
            CapBottom = _options.CapBottom
        };
        return new Polygon3D(scaledBase, newOptions);
    }

    public Polygon3D RotateX(double angle) => Rotate(new Vector3D(angle, 0, 0));
    public Polygon3D RotateY(double angle) => Rotate(new Vector3D(0, angle, 0));
    public Polygon3D RotateZ(double angle) => Rotate(new Vector3D(0, 0, angle));

    public Polygon3D Rotate(Vector3D eulerAngles)
    {
        var newOptions = new ExtrusionOptions
        {
            Height = _options.Height,
            TopScale = _options.TopScale,
            Rotation = _options.Rotation + eulerAngles,
            Position = _options.Position,
            CapTop = _options.CapTop,
            CapBottom = _options.CapBottom
        };
        return new Polygon3D(_basePolygon, newOptions);
    }

    /// <summary>
    /// Gets information about all edges.
    /// </summary>
    public IReadOnlyList<(Vector3D Start, Vector3D End, string Type)> GetEdges()
    {
        var edges = new List<(Vector3D Start, Vector3D End, string Type)>();
        var n = BottomVertexCount;

        // Bottom edges
        for (int i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            edges.Add((_vertices[i], _vertices[j], "Bottom"));
        }

        // Top edges
        for (int i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            edges.Add((_vertices[n + i], _vertices[n + j], "Top"));
        }

        // Vertical edges
        for (int i = 0; i < n; i++)
        {
            edges.Add((_vertices[i], _vertices[n + i], "Vertical"));
        }

        return edges;
    }

    private void GenerateGeometry()
    {
        var baseVertices = _basePolygon.Vertices;
        var n = baseVertices.Count;
        var baseCentroid = _basePolygon.Centroid;

        // Create rotation matrix
        var rotX = Matrix3x3.RotationX(_options.Rotation.X);
        var rotY = Matrix3x3.RotationY(_options.Rotation.Y);
        var rotZ = Matrix3x3.RotationZ(_options.Rotation.Z);
        var rotationMatrix = rotZ * rotY * rotX;

        // Generate bottom vertices
        foreach (var v in baseVertices)
        {
            var local = v - baseCentroid;
            var rotated = rotationMatrix.Transform(local);
            _vertices.Add(rotated + baseCentroid + _options.Position);
        }

        // Generate top vertices
        var topOffset = rotationMatrix.Transform(new Vector3D(0, 0, _options.Height));
        foreach (var v in baseVertices)
        {
            var local = (v - baseCentroid) * _options.TopScale;
            var rotated = rotationMatrix.Transform(local);
            _vertices.Add(rotated + baseCentroid + _options.Position + topOffset);
        }

        // Generate side triangles
        for (int i = 0; i < n; i++)
        {
            var j = (i + 1) % n;

            var bottomLeft = _vertices[i];
            var bottomRight = _vertices[j];
            var topLeft = _vertices[n + i];
            var topRight = _vertices[n + j];

            // Two triangles per side
            _triangles.Add(new Triangle(bottomLeft, bottomRight, topRight));
            _triangles.Add(new Triangle(bottomLeft, topRight, topLeft));

            // Calculate face normal for side
            var normal = Vector3D.Cross(bottomRight - bottomLeft, topLeft - bottomLeft).Normalized;
            _faceNormals.Add(normal);
        }

        // Generate bottom cap triangles
        if (_options.CapBottom)
        {
            var bottomTris = _basePolygon.Triangulate();
            foreach (var tri in bottomTris)
            {
                // Find corresponding vertices and reverse winding for bottom
                var i0 = FindVertexIndex(tri.V0, baseVertices);
                var i1 = FindVertexIndex(tri.V1, baseVertices);
                var i2 = FindVertexIndex(tri.V2, baseVertices);

                if (i0 >= 0 && i1 >= 0 && i2 >= 0)
                {
                    _triangles.Add(new Triangle(_vertices[i2], _vertices[i1], _vertices[i0]));
                }
            }
            _faceNormals.Add(-rotationMatrix.Transform(Vector3D.UnitZ));
        }

        // Generate top cap triangles
        if (_options.CapTop)
        {
            var topTris = _basePolygon.Triangulate();
            foreach (var tri in topTris)
            {
                var i0 = FindVertexIndex(tri.V0, baseVertices);
                var i1 = FindVertexIndex(tri.V1, baseVertices);
                var i2 = FindVertexIndex(tri.V2, baseVertices);

                if (i0 >= 0 && i1 >= 0 && i2 >= 0)
                {
                    _triangles.Add(new Triangle(_vertices[n + i0], _vertices[n + i1], _vertices[n + i2]));
                }
            }
            _faceNormals.Add(rotationMatrix.Transform(Vector3D.UnitZ));
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

    private double CalculateVolume()
    {
        // Use signed tetrahedron volume method
        double volume = 0;
        var reference = Centroid;

        foreach (var tri in _triangles)
        {
            var v0 = tri.V0 - reference;
            var v1 = tri.V1 - reference;
            var v2 = tri.V2 - reference;

            volume += Vector3D.Dot(v0, Vector3D.Cross(v1, v2)) / 6.0;
        }

        return Math.Abs(volume);
    }   
}
