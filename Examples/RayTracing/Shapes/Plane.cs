using System.Numerics;

namespace RayTracing.Shapes;

public class Plane : BaseShape
{
    public override float LocalClosestT(ref Ray ray)
    {
        if (Math.Abs(ray.Direction.Y) < 1e-5) return -1;
        return -ray.Origin.Y / ray.Direction.Y;
    }
    public override Vector3 Normal(Vector3 hitPoint) => Vector3.UnitY;
}
