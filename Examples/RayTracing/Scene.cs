using System.Numerics;

namespace RayTracing;
public class Material
{
    public Vector3 Albedo = Vector3.One;
    public float Roughness = 1.0f;
    public float Metallic = 0.0f;
    public Material() { }
}

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
