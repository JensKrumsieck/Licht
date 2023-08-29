using System.Numerics;

namespace RayTracing.Shapes;

public class Sphere : BaseShape
{
    public float Radius = .5f;

    public override float LocalClosestT(ref Ray ray)
    {
        var a = Vector3.Dot(ray.Direction, ray.Direction);
        var b = 2.0f * Vector3.Dot(ray.Origin, ray.Direction);
        var c = Vector3.Dot(ray.Origin, ray.Origin) - Radius * Radius;
        var discriminant = b * b - 4 * a * c;
        
        if (discriminant < 0) return -1;
        //var t0 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
        return (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
    }
    public override Vector3 Normal(Vector3 hitPoint) => Vector3.Normalize(hitPoint);
}
