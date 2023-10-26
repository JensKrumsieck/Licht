using System.Numerics;
using System.Runtime.InteropServices;

namespace ConsoleApp1;

[StructLayout(LayoutKind.Explicit)]
public struct Ubo
{
    [FieldOffset(0)] public Matrix4x4 Projection = Matrix4x4.Identity;
    [FieldOffset(64)] public Matrix4x4 View = Matrix4x4.Identity;
    [FieldOffset(128)] public Matrix4x4 InverseView = Matrix4x4.Identity;
    [FieldOffset(192)] public Vector4 AmbientLightColor = new(1, 1, 1, .02f);
    public Ubo(){ }
}
