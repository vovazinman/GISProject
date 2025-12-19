using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;

namespace GIS3DEngine.Core.Spatial;

/// <summary>
/// Coordinate transformer for WGS84, ECEF, and local coordinate systems.
/// </summary>
public class CoordinateTransformer : ICoordinateTransformer
{
    // WGS84 ellipsoid parameters
    public const double SemiMajorAxis = 6378137.0; // meters
    public const double SemiMinorAxis = 6356752.314245; // meters
    public const double Flattening = 1.0 / 298.257223563;
    public const double Eccentricity = 0.0818191908426;
    public const double EccentricitySquared = 0.00669437999014;

    /// <summary>
    /// Converts WGS84 coordinates to ECEF (Earth-Centered, Earth-Fixed) Cartesian.
    /// </summary>
    public Vector3D GeoToCartesian(GeoCoordinate coordinate)
    {
        var lat = coordinate.LatitudeRadians;
        var lon = coordinate.LongitudeRadians;
        var alt = coordinate.Altitude;

        var sinLat = Math.Sin(lat);
        var cosLat = Math.Cos(lat);
        var sinLon = Math.Sin(lon);
        var cosLon = Math.Cos(lon);

        // Radius of curvature in the prime vertical
        var N = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * sinLat * sinLat);

        var x = (N + alt) * cosLat * cosLon;
        var y = (N + alt) * cosLat * sinLon;
        var z = (N * (1 - EccentricitySquared) + alt) * sinLat;

        return new Vector3D(x, y, z);
    }

    /// <summary>
    /// Converts ECEF Cartesian coordinates to WGS84.
    /// </summary>
    public GeoCoordinate CartesianToGeo(Vector3D position)
    {
        var x = position.X;
        var y = position.Y;
        var z = position.Z;

        var lon = Math.Atan2(y, x);

        var p = Math.Sqrt(x * x + y * y);
        var lat = Math.Atan2(z, p * (1 - EccentricitySquared));

        // Iterative calculation for latitude
        for (int i = 0; i < 10; i++)
        {
            var sinLat = Math.Sin(lat);
            var N = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * sinLat * sinLat);
            lat = Math.Atan2(z + EccentricitySquared * N * sinLat, p);
        }

        var sinLatFinal = Math.Sin(lat);
        var N_final = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * sinLatFinal * sinLatFinal);
        var alt = p / Math.Cos(lat) - N_final;

        return new GeoCoordinate(
            lat * 180.0 / Math.PI,
            lon * 180.0 / Math.PI,
            alt
        );
    }

    /// <summary>
    /// Transforms a point from world to local coordinate system.
    /// </summary>
    public Vector3D WorldToLocal(Vector3D worldPosition, Vector3D origin, Vector3D rotation)
    {
        var translated = worldPosition - origin;

        // Apply inverse rotation (negative angles)
        var rotX = Matrix3x3.RotationX(-rotation.X);
        var rotY = Matrix3x3.RotationY(-rotation.Y);
        var rotZ = Matrix3x3.RotationZ(-rotation.Z);

        // Inverse order for inverse transformation
        var rotationMatrix = rotX * rotY * rotZ;
        return rotationMatrix.Transform(translated);
    }

    /// <summary>
    /// Transforms a point from local to world coordinate system.
    /// </summary>
    public Vector3D LocalToWorld(Vector3D localPosition, Vector3D origin, Vector3D rotation)
    {
        var rotX = Matrix3x3.RotationX(rotation.X);
        var rotY = Matrix3x3.RotationY(rotation.Y);
        var rotZ = Matrix3x3.RotationZ(rotation.Z);

        var rotationMatrix = rotZ * rotY * rotX;
        var rotated = rotationMatrix.Transform(localPosition);
        return rotated + origin;
    }

    /// <summary>
    /// Gets the local tangent plane (ENU - East, North, Up) at a location.
    /// </summary>
    public (Vector3D East, Vector3D North, Vector3D Up) GetLocalTangentPlane(GeoCoordinate location)
    {
        var lat = location.LatitudeRadians;
        var lon = location.LongitudeRadians;

        var sinLat = Math.Sin(lat);
        var cosLat = Math.Cos(lat);
        var sinLon = Math.Sin(lon);
        var cosLon = Math.Cos(lon);

        // East vector
        var east = new Vector3D(-sinLon, cosLon, 0);

        // North vector
        var north = new Vector3D(-sinLat * cosLon, -sinLat * sinLon, cosLat);

        // Up vector
        var up = new Vector3D(cosLat * cosLon, cosLat * sinLon, sinLat);

        return (east, north, up);
    }

    /// <summary>
    /// Converts a geographic offset to local ENU coordinates.
    /// </summary>
    public Vector3D GeoToENU(GeoCoordinate point, GeoCoordinate reference)
    {
        var pointECEF = GeoToCartesian(point);
        var refECEF = GeoToCartesian(reference);
        var delta = pointECEF - refECEF;

        var (east, north, up) = GetLocalTangentPlane(reference);

        return new Vector3D(
            Vector3D.Dot(delta, east),
            Vector3D.Dot(delta, north),
            Vector3D.Dot(delta, up)
        );
    }
}

/// <summary>
/// Distance and bearing calculations for geographic coordinates.
/// </summary>
public class DistanceCalculator
{
    private const double EarthRadius = 6371000; // meters

    /// <summary>
    /// Calculates great-circle distance using Haversine formula.
    /// Fast but less accurate for large distances.
    /// </summary>
    public double HaversineDistance(GeoCoordinate a, GeoCoordinate b)
    {
        var dLat = b.LatitudeRadians - a.LatitudeRadians;
        var dLon = b.LongitudeRadians - a.LongitudeRadians;

        var sinDLat = Math.Sin(dLat / 2);
        var sinDLon = Math.Sin(dLon / 2);

        var h = sinDLat * sinDLat +
                Math.Cos(a.LatitudeRadians) * Math.Cos(b.LatitudeRadians) *
                sinDLon * sinDLon;

        return 2 * EarthRadius * Math.Asin(Math.Sqrt(h));
    }

    /// <summary>
    /// Calculates distance using Vincenty's formula.
    /// More accurate for all distances, especially long ones.
    /// </summary>
    public double VincentyDistance(GeoCoordinate a, GeoCoordinate b)
    {
        const double a_axis = CoordinateTransformer.SemiMajorAxis;
        const double b_axis = CoordinateTransformer.SemiMinorAxis;
        const double f = CoordinateTransformer.Flattening;

        var L = b.LongitudeRadians - a.LongitudeRadians;
        var U1 = Math.Atan((1 - f) * Math.Tan(a.LatitudeRadians));
        var U2 = Math.Atan((1 - f) * Math.Tan(b.LatitudeRadians));

        var sinU1 = Math.Sin(U1);
        var cosU1 = Math.Cos(U1);
        var sinU2 = Math.Sin(U2);
        var cosU2 = Math.Cos(U2);

        var lambda = L;
        double lambdaP;
        int iterations = 0;
        double sinSigma, cosSigma, sigma, sinAlpha, cosSqAlpha, cos2SigmaM, C;

        do
        {
            var sinLambda = Math.Sin(lambda);
            var cosLambda = Math.Cos(lambda);

            sinSigma = Math.Sqrt(
                Math.Pow(cosU2 * sinLambda, 2) +
                Math.Pow(cosU1 * sinU2 - sinU1 * cosU2 * cosLambda, 2)
            );

            if (sinSigma == 0)
                return 0; // Co-incident points

            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Math.Atan2(sinSigma, cosSigma);

            sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cosSqAlpha = 1 - sinAlpha * sinAlpha;

            cos2SigmaM = cosSqAlpha != 0 ? cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha : 0;

            C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));

            lambdaP = lambda;
            lambda = L + (1 - C) * f * sinAlpha * (
                sigma + C * sinSigma * (
                    cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)
                )
            );

            iterations++;
        } while (Math.Abs(lambda - lambdaP) > 1e-12 && iterations < 100);

        if (iterations >= 100)
            return HaversineDistance(a, b); // Fallback

        var uSq = cosSqAlpha * (a_axis * a_axis - b_axis * b_axis) / (b_axis * b_axis);
        var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));

        var deltaSigma = B * sinSigma * (
            cos2SigmaM + B / 4 * (
                cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)
            )
        );

        return b_axis * A * (sigma - deltaSigma);
    }

    /// <summary>
    /// Calculates initial bearing (azimuth) from point a to point b.
    /// Returns bearing in radians (0 = North, π/2 = East).
    /// </summary>
    public double InitialBearing(GeoCoordinate from, GeoCoordinate to)
    {
        var dLon = to.LongitudeRadians - from.LongitudeRadians;

        var y = Math.Sin(dLon) * Math.Cos(to.LatitudeRadians);
        var x = Math.Cos(from.LatitudeRadians) * Math.Sin(to.LatitudeRadians) -
                Math.Sin(from.LatitudeRadians) * Math.Cos(to.LatitudeRadians) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x);

        // Normalize to 0-2π
        return (bearing + 2 * Math.PI) % (2 * Math.PI);
    }

    /// <summary>
    /// Calculates destination point given start, bearing, and distance.
    /// </summary>
    public GeoCoordinate DestinationPoint(GeoCoordinate start, double bearing, double distance)
    {
        var angularDistance = distance / EarthRadius;
        var lat1 = start.LatitudeRadians;
        var lon1 = start.LongitudeRadians;

        var sinLat1 = Math.Sin(lat1);
        var cosLat1 = Math.Cos(lat1);
        var sinAngDist = Math.Sin(angularDistance);
        var cosAngDist = Math.Cos(angularDistance);
        var sinBearing = Math.Sin(bearing);
        var cosBearing = Math.Cos(bearing);

        var lat2 = Math.Asin(sinLat1 * cosAngDist + cosLat1 * sinAngDist * cosBearing);
        var lon2 = lon1 + Math.Atan2(
            sinBearing * sinAngDist * cosLat1,
            cosAngDist - sinLat1 * Math.Sin(lat2)
        );

        return new GeoCoordinate(
            lat2 * 180.0 / Math.PI,
            lon2 * 180.0 / Math.PI,
            start.Altitude
        );
    }

    /// <summary>
    /// Calculates the midpoint between two coordinates.
    /// </summary>
    public GeoCoordinate Midpoint(GeoCoordinate a, GeoCoordinate b)
    {
        var lat1 = a.LatitudeRadians;
        var lon1 = a.LongitudeRadians;
        var lat2 = b.LatitudeRadians;
        var dLon = b.LongitudeRadians - a.LongitudeRadians;

        var Bx = Math.Cos(lat2) * Math.Cos(dLon);
        var By = Math.Cos(lat2) * Math.Sin(dLon);

        var lat3 = Math.Atan2(
            Math.Sin(lat1) + Math.Sin(lat2),
            Math.Sqrt((Math.Cos(lat1) + Bx) * (Math.Cos(lat1) + Bx) + By * By)
        );
        var lon3 = lon1 + Math.Atan2(By, Math.Cos(lat1) + Bx);

        return new GeoCoordinate(
            lat3 * 180.0 / Math.PI,
            lon3 * 180.0 / Math.PI,
            (a.Altitude + b.Altitude) / 2
        );
    }
}

/// <summary>
/// Spatial query operations for geometric analysis.
/// </summary>
public class SpatialQuery : ISpatialQuery
{
    private readonly CoordinateTransformer _transformer;
    private readonly DistanceCalculator _distanceCalculator;

    public SpatialQuery(CoordinateTransformer? transformer = null)
    {
        _transformer = transformer ?? new CoordinateTransformer();
        _distanceCalculator = new DistanceCalculator();
    }

    /// <summary>
    /// Tests if a point is inside a 2D polygon using ray casting.
    /// </summary>
    public bool PointInPolygon(Vector3D point, IReadOnlyList<Vector3D> polygonVertices)
    {
        if (polygonVertices.Count < 3)
            return false;

        int crossings = 0;
        for (int i = 0; i < polygonVertices.Count; i++)
        {
            var j = (i + 1) % polygonVertices.Count;
            var vi = polygonVertices[i];
            var vj = polygonVertices[j];

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
    /// Tests if a geographic point is inside a polygon defined by geo coordinates.
    /// </summary>
    public bool GeoPointInPolygon(GeoCoordinate point, IReadOnlyList<GeoCoordinate> polygonCoordinates)
    {
        var vertices = polygonCoordinates
            .Select(c => new Vector3D(c.Longitude, c.Latitude, 0))
            .ToList();
        var testPoint = new Vector3D(point.Longitude, point.Latitude, 0);
        return PointInPolygon(testPoint, vertices);
    }

    public double DistanceBetween(GeoCoordinate a, GeoCoordinate b) =>
        _distanceCalculator.VincentyDistance(a, b);

    public double BearingBetween(GeoCoordinate from, GeoCoordinate to) =>
        _distanceCalculator.InitialBearing(from, to);

    public double HeightAbovePolygon(Vector3D point, IGeometry3D polygon)
    {
        if (polygon.Vertices.Count < 3)
            return point.Z;

        var plane = Plane.FromPoints(
            polygon.Vertices[0],
            polygon.Vertices[1],
            polygon.Vertices[2]
        );

        return plane.SignedDistance(point);
    }

    /// <summary>
    /// Finds the closest point on polygon edges to a test point.
    /// </summary>
    public Vector3D ClosestPointOnPolygonEdge(Vector3D point, IReadOnlyList<Vector3D> polygonVertices)
    {
        var closest = polygonVertices[0];
        var minDist = double.MaxValue;

        for (int i = 0; i < polygonVertices.Count; i++)
        {
            var j = (i + 1) % polygonVertices.Count;
            var edgePoint = ClosestPointOnSegment(point, polygonVertices[i], polygonVertices[j]);
            var dist = Vector3D.Distance(point, edgePoint);

            if (dist < minDist)
            {
                minDist = dist;
                closest = edgePoint;
            }
        }

        return closest;
    }

    /// <summary>
    /// Calculates distance from point to polygon edge.
    /// </summary>
    public double DistanceToPolygonEdge(Vector3D point, IReadOnlyList<Vector3D> polygonVertices)
    {
        var closest = ClosestPointOnPolygonEdge(point, polygonVertices);
        return Vector3D.Distance(point, closest);
    }

    /// <summary>
    /// Tests if two polygons intersect.
    /// </summary>
    public bool PolygonsIntersect(IReadOnlyList<Vector3D> polygonA, IReadOnlyList<Vector3D> polygonB)
    {
        // Check if any vertex of A is inside B
        foreach (var v in polygonA)
        {
            if (PointInPolygon(v, polygonB))
                return true;
        }

        // Check if any vertex of B is inside A
        foreach (var v in polygonB)
        {
            if (PointInPolygon(v, polygonA))
                return true;
        }

        // Check edge intersections
        for (int i = 0; i < polygonA.Count; i++)
        {
            var a1 = polygonA[i];
            var a2 = polygonA[(i + 1) % polygonA.Count];

            for (int j = 0; j < polygonB.Count; j++)
            {
                var b1 = polygonB[j];
                var b2 = polygonB[(j + 1) % polygonB.Count];

                if (SegmentsIntersect(a1, a2, b1, b2))
                    return true;
            }
        }

        return false;
    }

    private static Vector3D ClosestPointOnSegment(Vector3D point, Vector3D a, Vector3D b)
    {
        var ab = b - a;
        var ap = point - a;
        var t = Vector3D.Dot(ap, ab) / Vector3D.Dot(ab, ab);
        t = Math.Clamp(t, 0, 1);
        return a + ab * t;
    }

    private static bool SegmentsIntersect(Vector3D a1, Vector3D a2, Vector3D b1, Vector3D b2)
    {
        var d1 = CrossProduct2D(b2 - b1, a1 - b1);
        var d2 = CrossProduct2D(b2 - b1, a2 - b1);
        var d3 = CrossProduct2D(a2 - a1, b1 - a1);
        var d4 = CrossProduct2D(a2 - a1, b2 - a1);

        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
               ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double CrossProduct2D(Vector3D a, Vector3D b) => a.X * b.Y - a.Y * b.X;
}

/// <summary>
/// Polygon validation operations.
/// </summary>
public class PolygonValidator : IPolygonValidator
{
    public ValidationResult Validate(IReadOnlyList<Vector3D> vertices)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check minimum vertices
        if (vertices.Count < 3)
        {
            return ValidationResult.Invalid("Polygon must have at least 3 vertices");
        }

        // Check for self-intersection
        var hasSelfIntersection = IsSelfIntersecting(vertices);
        if (hasSelfIntersection)
        {
            errors.Add("Polygon has self-intersecting edges");
        }

        // Get winding order
        var windingOrder = GetWindingOrder(vertices);
        if (windingOrder == WindingOrder.Degenerate)
        {
            errors.Add("Polygon has zero or near-zero area (degenerate)");
        }

        // Check convexity
        var isConvex = IsConvex(vertices);

        // Check for near-degenerate triangles
        var area = CalculateArea(vertices);
        if (area < 1e-6 && area > 0)
        {
            warnings.Add("Polygon has very small area");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            IsConvex = isConvex,
            HasSelfIntersection = hasSelfIntersection,
            WindingOrder = windingOrder,
            Errors = errors,
            Warnings = warnings
        };
    }

    public bool IsConvex(IReadOnlyList<Vector3D> vertices)
    {
        if (vertices.Count < 3)
            return false;

        bool? sign = null;
        for (int i = 0; i < vertices.Count; i++)
        {
            var v0 = vertices[i];
            var v1 = vertices[(i + 1) % vertices.Count];
            var v2 = vertices[(i + 2) % vertices.Count];

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

    public bool IsSelfIntersecting(IReadOnlyList<Vector3D> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var a1 = vertices[i];
            var a2 = vertices[(i + 1) % vertices.Count];

            for (int j = i + 2; j < vertices.Count; j++)
            {
                if (i == 0 && j == vertices.Count - 1)
                    continue;

                var b1 = vertices[j];
                var b2 = vertices[(j + 1) % vertices.Count];

                if (SegmentsIntersect(a1, a2, b1, b2))
                    return true;
            }
        }
        return false;
    }

    public WindingOrder GetWindingOrder(IReadOnlyList<Vector3D> vertices)
    {
        var signedArea = CalculateSignedArea(vertices);
        if (Math.Abs(signedArea) < 1e-10)
            return WindingOrder.Degenerate;
        return signedArea > 0 ? WindingOrder.CounterClockwise : WindingOrder.Clockwise;
    }

    private static double CalculateSignedArea(IReadOnlyList<Vector3D> vertices)
    {
        double area = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            var j = (i + 1) % vertices.Count;
            area += vertices[i].X * vertices[j].Y;
            area -= vertices[j].X * vertices[i].Y;
        }
        return area / 2;
    }

    private static double CalculateArea(IReadOnlyList<Vector3D> vertices) =>
        Math.Abs(CalculateSignedArea(vertices));

    private static bool SegmentsIntersect(Vector3D a1, Vector3D a2, Vector3D b1, Vector3D b2)
    {
        var d1 = CrossProduct2D(b2 - b1, a1 - b1);
        var d2 = CrossProduct2D(b2 - b1, a2 - b1);
        var d3 = CrossProduct2D(a2 - a1, b1 - a1);
        var d4 = CrossProduct2D(a2 - a1, b2 - a1);

        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
               ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double CrossProduct2D(Vector3D a, Vector3D b) => a.X * b.Y - a.Y * b.X;
}
