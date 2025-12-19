using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;

namespace GIS3DEngine.Core.Geometry;

/// <summary>
/// Options for polygon extrusion.
/// </summary>
public class ExtrusionOptions
{
    public double Height { get; set; } = 1.0;
    public double TopScale { get; set; } = 1.0;
    public Vector3D Rotation { get; set; } = Vector3D.Zero;
    public Vector3D Position { get; set; } = Vector3D.Zero;
    public bool CapTop { get; set; } = true;
    public bool CapBottom { get; set; } = true;
}

/// <summary>
/// Immutable 2D polygon with validation, triangulation, and geometric properties.
/// </summary>
public class Polygon2D
{
    private readonly List<Vector3D> _vertices;
    private List<Triangle>? _triangles;
    private WindingOrder? _windingOrder;
    private bool? _isConvex;
    private double? _area;
    private Vector3D? _centroid;
    private BoundingBox? _bounds;

    public IReadOnlyList<Vector3D> Vertices => _vertices.AsReadOnly();
    public int VertexCount => _vertices.Count;

    private Polygon2D(IEnumerable<Vector3D> vertices)
    {
        _vertices = RemoveDuplicates(vertices.ToList());
        if (_vertices.Count < 3)
            throw new ArgumentException("Polygon must have at least 3 vertices");
    }

    /// <summary>
    /// Creates polygon from vertex list.
    /// </summary>
    public static Polygon2D FromVertices(IEnumerable<Vector3D> vertices) => new(vertices);

    /// <summary>
    /// Creates polygon from GeoCoordinates (using local X=lon, Y=lat mapping).
    /// </summary>
    public static Polygon2D FromGeoCoordinates(IEnumerable<GeoCoordinate> coordinates)
    {
        var vertices = coordinates.Select(c => new Vector3D(c.Longitude, c.Latitude, c.Altitude));
        return new Polygon2D(vertices);
    }

    /// <summary>
    /// Creates a regular polygon with n sides.
    /// </summary>
    public static Polygon2D CreateRegular(int sides, double radius, Vector3D center = default)
    {
        if (sides < 3)
            throw new ArgumentException("Regular polygon must have at least 3 sides");

        var vertices = new List<Vector3D>();
        var angleStep = 2 * Math.PI / sides;

        for (int i = 0; i < sides; i++)
        {
            var angle = i * angleStep - Math.PI / 2; // Start from top
            vertices.Add(new Vector3D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle),
                center.Z
            ));
        }

        return new Polygon2D(vertices);
    }

    /// <summary>
    /// Gets the winding order (CCW, CW, or degenerate).
    /// </summary>
    public WindingOrder WindingOrder
    {
        get
        {
            _windingOrder ??= CalculateWindingOrder();
            return _windingOrder.Value;
        }
    }

    /// <summary>
    /// Gets whether the polygon is convex.
    /// </summary>
    public bool IsConvex
    {
        get
        {
            _isConvex ??= CalculateConvexity();
            return _isConvex.Value;
        }
    }

    /// <summary>
    /// Gets the signed area (positive for CCW, negative for CW).
    /// </summary>
    public double SignedArea
    {
        get
        {
            double area = 0;
            for (int i = 0; i < _vertices.Count; i++)
            {
                var j = (i + 1) % _vertices.Count;
                area += _vertices[i].X * _vertices[j].Y;
                area -= _vertices[j].X * _vertices[i].Y;
            }
            return area / 2;
        }
    }

    /// <summary>
    /// Gets the absolute area.
    /// </summary>
    public double Area
    {
        get
        {
            _area ??= Math.Abs(SignedArea);
            return _area.Value;
        }
    }

    /// <summary>
    /// Gets the centroid.
    /// </summary>
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

    /// <summary>
    /// Gets the bounding box.
    /// </summary>
    public BoundingBox Bounds
    {
        get
        {
            _bounds ??= BoundingBox.FromPoints(_vertices);
            return _bounds.Value;
        }
    }

    /// <summary>
    /// Gets the base plane of the polygon.
    /// </summary>
    public Plane BasePlane => Plane.FromPoints(_vertices[0], _vertices[1], _vertices[2]);

    /// <summary>
    /// Gets all edges as vectors.
    /// </summary>
    public IReadOnlyList<Vector3D> GetEdges()
    {
        var edges = new List<Vector3D>();
        for (int i = 0; i < _vertices.Count; i++)
        {
            var j = (i + 1) % _vertices.Count;
            edges.Add(_vertices[j] - _vertices[i]);
        }
        return edges;
    }

    /// <summary>
    /// Gets edge at index as tuple (start, end).
    /// </summary>
    public (Vector3D Start, Vector3D End) GetEdge(int index)
    {
        var j = (index + 1) % _vertices.Count;
        return (_vertices[index], _vertices[j]);
    }

    /// <summary>
    /// Tests if a point is inside the polygon using ray casting.
    /// </summary>
    public bool ContainsPoint(Vector3D point)
    {
        int crossings = 0;
        for (int i = 0; i < _vertices.Count; i++)
        {
            var j = (i + 1) % _vertices.Count;
            var vi = _vertices[i];
            var vj = _vertices[j];

            if ((vi.Y <= point.Y && vj.Y > point.Y) || (vj.Y <= point.Y && vi.Y > point.Y))
            {
                var t = (point.Y - vi.Y) / (vj.Y - vi.Y);
                if (point.X < vi.X + t * (vj.X - vi.X))
                    crossings++;
            }
        }
        return crossings % 2 == 1;
    }

    /// <summary>
    /// Triangulates the polygon using ear clipping algorithm.
    /// </summary>
    public IReadOnlyList<Triangle> Triangulate()
    {
        if (_triangles != null)
            return _triangles;

        _triangles = new List<Triangle>();
        if (_vertices.Count < 3)
            return _triangles;

        if (_vertices.Count == 3)
        {
            _triangles.Add(new Triangle(_vertices[0], _vertices[1], _vertices[2]));
            return _triangles;
        }

        // Use ear clipping algorithm
        var remaining = new List<int>(Enumerable.Range(0, _vertices.Count));
        var vertices = _vertices.ToList();

        // Ensure CCW winding for ear clipping
        if (WindingOrder == WindingOrder.Clockwise)
            remaining.Reverse();

        int maxIterations = remaining.Count * remaining.Count;
        int iterations = 0;

        while (remaining.Count > 3 && iterations < maxIterations)
        {
            iterations++;
            bool earFound = false;

            for (int i = 0; i < remaining.Count; i++)
            {
                var prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                var curr = remaining[i];
                var next = remaining[(i + 1) % remaining.Count];

                if (IsEar(vertices, remaining, prev, curr, next))
                {
                    _triangles.Add(new Triangle(vertices[prev], vertices[curr], vertices[next]));
                    remaining.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound)
                break;
        }

        // Add final triangle
        if (remaining.Count == 3)
        {
            _triangles.Add(new Triangle(
                vertices[remaining[0]],
                vertices[remaining[1]],
                vertices[remaining[2]]
            ));
        }

        return _triangles;
    }

    /// <summary>
    /// Reverses the winding order.
    /// </summary>
    public Polygon2D ReverseWinding()
    {
        var reversed = _vertices.ToList();
        reversed.Reverse();
        return new Polygon2D(reversed);
    }

    /// <summary>
    /// Ensures counter-clockwise winding order.
    /// </summary>
    public Polygon2D EnsureCounterClockwise() =>
        WindingOrder == WindingOrder.Clockwise ? ReverseWinding() : this;

    /// <summary>
    /// Translates the polygon.
    /// </summary>
    public Polygon2D Translate(Vector3D offset) =>
        new(_vertices.Select(v => v + offset));

    /// <summary>
    /// Scales the polygon around its centroid.
    /// </summary>
    public Polygon2D Scale(double factor)
    {
        var center = Centroid;
        return new Polygon2D(_vertices.Select(v =>
            center + (v - center) * factor
        ));
    }

    /// <summary>
    /// Extrudes the polygon to create a 3D shape.
    /// </summary>
    public Polygon3D Extrude(ExtrusionOptions? options = null) =>
        Polygon3D.Extrude(this, options ?? new ExtrusionOptions());

    private static List<Vector3D> RemoveDuplicates(List<Vector3D> vertices)
    {
        var result = new List<Vector3D>();
        foreach (var v in vertices)
        {
            if (result.Count == 0 || Vector3D.Distance(result[^1], v) > 1e-10)
                result.Add(v);
        }
        // Check first and last
        if (result.Count > 1 && Vector3D.Distance(result[0], result[^1]) < 1e-10)
            result.RemoveAt(result.Count - 1);
        return result;
    }

    private WindingOrder CalculateWindingOrder()
    {
        var signedArea = SignedArea;
        if (Math.Abs(signedArea) < 1e-10)
            return WindingOrder.Degenerate;
        return signedArea > 0 ? WindingOrder.CounterClockwise : WindingOrder.Clockwise;
    }

    private bool CalculateConvexity()
    {
        if (_vertices.Count < 3)
            return false;

        bool? sign = null;
        for (int i = 0; i < _vertices.Count; i++)
        {
            var v0 = _vertices[i];
            var v1 = _vertices[(i + 1) % _vertices.Count];
            var v2 = _vertices[(i + 2) % _vertices.Count];

            var cross = (v1.X - v0.X) * (v2.Y - v1.Y) - (v1.Y - v0.Y) * (v2.X - v1.X);

            if (Math.Abs(cross) < 1e-10)
                continue;

            var currentSign = cross > 0;
            if (!sign.HasValue)
                sign = currentSign;
            else if (sign.Value != currentSign)
                return false;
        }

        return true;
    }

    private static bool IsEar(List<Vector3D> vertices, List<int> remaining, int prev, int curr, int next)
    {
        var a = vertices[prev];
        var b = vertices[curr];
        var c = vertices[next];

        // Check if convex (CCW order)
        var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
        if (cross <= 0)
            return false;

        // Check if any other vertex is inside the triangle
        var triangle = new Triangle(a, b, c);
        foreach (var idx in remaining)
        {
            if (idx == prev || idx == curr || idx == next)
                continue;

            if (triangle.Contains(vertices[idx]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if polygon self-intersects.
    /// </summary>
    public bool IsSelfIntersecting()
    {
        for (int i = 0; i < _vertices.Count; i++)
        {
            var (a1, a2) = GetEdge(i);

            for (int j = i + 2; j < _vertices.Count; j++)
            {
                if (i == 0 && j == _vertices.Count - 1)
                    continue; // Adjacent edges

                var (b1, b2) = GetEdge(j);

                if (SegmentsIntersect(a1, a2, b1, b2))
                    return true;
            }
        }
        return false;
    }

    private static bool SegmentsIntersect(Vector3D a1, Vector3D a2, Vector3D b1, Vector3D b2)
    {
        var d1 = CrossProduct2D(b2 - b1, a1 - b1);
        var d2 = CrossProduct2D(b2 - b1, a2 - b1);
        var d3 = CrossProduct2D(a2 - a1, b1 - a1);
        var d4 = CrossProduct2D(a2 - a1, b2 - a1);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return false;
    }

    private static double CrossProduct2D(Vector3D a, Vector3D b) => a.X * b.Y - a.Y * b.X;
}
