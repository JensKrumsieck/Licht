using System.Numerics;

namespace RayTracing;

internal struct HitPayload
{
    public float HitDistance;
    public Vector3 WorldPosition;
    public Vector3 WorldNormal;
    public int ObjectIndex;
}
