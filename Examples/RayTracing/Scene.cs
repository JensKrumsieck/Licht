using System.Numerics;

namespace RayTracing;

public class Sphere
{
    public Vector3 Position = Vector3.Zero;
    public float Radius = .5f;
    public Vector3 Albedo = Vector3.One;
    public Sphere() {}
    public Sphere(Vector3 position, float radius, Vector3 albedo)
    {
        Position = position;
        Radius = radius;
        Albedo = albedo;
    }
}

public class Scene
{
    public readonly List<Sphere> Spheres = new();
}
