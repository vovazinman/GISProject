using GIS3DEngine.Core.Primitives;

namespace GIS3DEngine.Application.Dtos.Common;

public record Vector3Dto
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; }

    public static Vector3Dto From(Vector3D v) => new()
    {
        X = v.X,
        Y = v.Y,
        Z = v.Z
    };

    public Vector3D ToVector3D() => new(X, Y, Z);
}