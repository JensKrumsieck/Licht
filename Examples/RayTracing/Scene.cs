using RayTracing.Shapes;

namespace RayTracing;

public class Scene
{
    public readonly List<BaseShape> Objects = new();
    public readonly List<Material> Materials = new();
}
