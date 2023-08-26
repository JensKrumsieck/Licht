using System.Numerics;
using System.Runtime.CompilerServices;

namespace RayTracing;

public static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ConvertToAbgr(ref Vector4 color)
    {
        var r = (uint)(color.X * 255.0f);
        var g = (uint)(color.Y * 255.0f);
        var b = (uint)(color.Z * 255.0f);
        var a = (uint)(color.W * 255.0f);
        return (a << 24) | (b << 16) | (g << 8) | r;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion CrossProduct(Quaternion q1, Quaternion q2) => new(
        q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
        q1.W * q2.Y + q1.Y * q2.W + q1.Z * q2.X - q1.X * q2.Z,
        q1.W * q2.Z + q1.Z * q2.W + q1.X * q2.Y - q1.Y * q2.X,
        q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z
    );
}
