using System.Numerics;

namespace RayTracing;

public readonly ref struct Ray
{
    public readonly ref Vector3 Origin;
    public readonly ref Vector3 Direction;
    public Ray(ref Vector3 origin, ref Vector3 direction)
    {
        Direction = ref direction;
        Origin = ref origin;
    }
}
