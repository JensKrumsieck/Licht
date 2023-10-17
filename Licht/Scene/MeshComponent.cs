using Licht.Rendering;

namespace Licht.Scene;

public readonly struct MeshComponent : IDisposable
{
    public readonly Mesh Mesh;

    public MeshComponent(Mesh mesh)
    {
        Mesh = mesh;
    }
    
    public void Dispose() => Mesh.Dispose();
}
