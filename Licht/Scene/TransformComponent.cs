using System.Numerics;

namespace Licht.Scene;

public struct TransformComponent 
{
    public Vector3 Translation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Vector3 Rotation = Vector3.Zero;
    public TransformComponent() { }
}
