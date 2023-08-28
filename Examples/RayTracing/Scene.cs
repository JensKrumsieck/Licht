using System.Numerics;

namespace RayTracing;

public class Sphere
{
    public Vector3 Position = Vector3.Zero;
    public float Radius = .5f;
    public int MaterialIndex;
    public Sphere() {}
}

public class Scene
{
    public readonly List<Sphere> Spheres = new();
    public readonly List<Material> Materials = new();
}
