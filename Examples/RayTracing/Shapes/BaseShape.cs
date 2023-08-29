using System.Numerics;

namespace RayTracing.Shapes;

public abstract class BaseShape
{
    public Vector3 Position = Vector3.Zero;
    public int MaterialIndex;

    public float ClosestT(ref Ray ray)
    {
        var localRay = new Ray(ray.Origin - Position, ray.Direction);
        return LocalClosestT(ref localRay);
    }
    
    public abstract float LocalClosestT(ref Ray ray);
    public abstract Vector3 Normal(Vector3 hitPoint);
}
