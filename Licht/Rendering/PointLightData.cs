using System.Numerics;

namespace Licht.Rendering;

public struct PointLightData
{
    public Vector4 Color; //w is intensity
    public Vector4 PositionRadius; //w is radius
}