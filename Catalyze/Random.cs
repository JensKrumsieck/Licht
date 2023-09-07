using System.Numerics;
using System.Runtime.CompilerServices;

namespace Catalyze;

public static class Random
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PcgHash(ref uint seed)
    {
        var state  = seed * 747796405u + 2891336453u;
        var word = ((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u;
        return (word >> 22) ^ word;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Float(ref uint seed)
    {
        seed = PcgHash(ref seed);
        return seed / (float) uint.MaxValue;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Float(ref uint seed, float min, float max)
    {
        var f = Float(ref seed);
        return f * (max - min) + min;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Vec3(ref uint seed, float min, float max) =>
        new(Float(ref seed) * (max - min) + min, 
            Float(ref seed) * (max - min) + min,
            Float(ref seed) * (max - min) + min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Vec3(ref uint seed) =>
        new(Float(ref seed),
            Float(ref seed),
            Float(ref seed));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 InUnitSphere(ref uint seed) => Vector3.Normalize(Vec3(ref seed, -1, 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 OnHemisphere(ref uint seed, Vector3 normal)
    {
        var inUnitSphere = InUnitSphere(ref seed);
        return Vector3.Dot(inUnitSphere, normal) > 0 ? inUnitSphere : -inUnitSphere;
    }
}
