using System.Numerics;

namespace Licht.Rendering;

public struct TransformationNormalPushConstants
{
    public Matrix4x4 ModelMatrix;
    public Matrix4x4 NormalMatrix;
}
