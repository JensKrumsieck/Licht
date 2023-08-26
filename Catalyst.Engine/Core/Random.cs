using System.Numerics;

namespace Catalyst.Engine.Core;

public static class Random
{
    private static uint PcgHash(uint seed)
    {
        var state = seed * 747796405u + 2891336453u;
        var word = ((state >> (int)((state >>  28) + 4u))) ^ state * 277803737u;
        return (word >> 22) ^ word;
    }
    private static System.Random _random = new();

    public static uint UInt(ref uint seed) => PcgHash(seed);

    public static float Float(ref uint seed)
    {
        seed = PcgHash(seed);
        return seed / float.MaxValue;
    }

    public static Vector3 Vector3(ref uint seed) => new(Float(ref seed), Float(ref seed), Float(ref seed));
    
    public static Vector3 Vector3(ref uint seed, float min, float max) => new Vector3(
        Float(ref seed) * (max - min) + min,
        Float(ref seed) * (max - min) + min, 
        Float(ref seed) * (max - min) + min);

    public static Vector3 InUnitSphere(ref uint seed) => System.Numerics.Vector3.Normalize(Vector3(ref seed, -1, 1));
}
