using System.Numerics;

namespace Licht.Scene;

public struct TransformComponent 
{
    public Vector3 Translation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Vector3 Rotation = Vector3.Zero;
    public TransformComponent() { }
    
    public Matrix4x4 TransformationMatrix()
    {
        var c3 = MathF.Cos(Rotation.Z);
        var s3 = MathF.Sin(Rotation.Z);
        var c2 = MathF.Cos(Rotation.X);
        var s2 = MathF.Sin(Rotation.X);
        var c1 = MathF.Cos(Rotation.Y);
        var s1 = MathF.Sin(Rotation.Y);
        return new Matrix4x4(
            Scale.X * (c1 * c3 + s1 * s2 * s3), Scale.X * (c2 * s3), Scale.X * (c1 * s2 * s3 - c3 * s1), 0,
            Scale.Y * (c3 * s1 * s2 - c1 * s3), Scale.Y * (c2 * c3), Scale.Y * (c1 * c3 * s2 + s1 * s3), 0,
            Scale.Z * (c2 * s1), Scale.Z * (-s2), Scale.Z * (c1 * c2), 0,
            Translation.X, Translation.Y, Translation.Z, 1);
    }

    public Matrix4x4 NormalMatrix()
    {
        var c3 = MathF.Cos(Rotation.Z);
        var s3 = MathF.Sin(Rotation.Z);
        var c2 = MathF.Cos(Rotation.X);
        var s2 = MathF.Sin(Rotation.X);
        var c1 = MathF.Cos(Rotation.Y);
        var s1 = MathF.Sin(Rotation.Y);
        var invScale = new Vector3(1 / Scale.X, 1 / Scale.Y, 1 / Scale.Z);
        return new Matrix4x4(
            invScale.X * (c1 * c3 + s1 * s2 * s3), invScale.X * (c2 * s3), invScale.X * (c1 * s2 * s3 - c3 * s1), 0,
            invScale.Y * (c3 * s1 * s2 - c1 * s3), invScale.Y * (c2 * c3), invScale.Y * (c1 * c3 * s2 + s1 * s3), 0,
            invScale.Z * (c2 * s1), 1 / invScale.Z * (-s2), invScale.Z * (c1 * c2), 0,
            0, 0, 0, 1);
    }
}
