using System.Numerics;

namespace RayTracing;

public class Material
{
    public Vector3 Albedo = Vector3.One;
    public float Metallic = 0.0f;
    public float Roughness = 1.0f;
    public float Specular = 0.5f;
    public float EmissionPower = 0.0f;
    public Vector3 EmissionColor = Vector3.Zero;
    
    public Vector3 Emission => EmissionColor * EmissionPower;
    
    public Material() { }
}
