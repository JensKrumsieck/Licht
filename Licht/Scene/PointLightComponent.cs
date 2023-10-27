using System.Numerics;

namespace Licht.Scene;

public struct PointLightComponent
{
    public Vector3 Color = Vector3.One;
    public float Intensity = 1.0f;

    public PointLightComponent(float intensity, Vector3 color) : this(intensity) => Color = color;
    public PointLightComponent(float intensity) => Intensity = intensity;
}