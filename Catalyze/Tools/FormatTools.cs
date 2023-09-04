using System.Numerics;
using Silk.NET.Vulkan;

namespace Catalyze.Tools;

public static class FormatTools
{
    public static Format FormatOf(Type t)
    {
        if (t == typeof(float)) return Format.R32Sfloat;
        if (t == typeof(Vector2)) return Format.R32G32Sfloat;
        if (t == typeof(Vector3)) return Format.R32G32B32Sfloat;
        if (t == typeof(Vector4)) return Format.R32G32B32A32Sfloat;
        return Format.Undefined;
    }

    public static Format FormatOf<T>() => FormatOf(typeof(T));

    public static uint SizeOf(Format format) => format switch
    {
        Format.Undefined => 0,
        Format.R8G8B8Unorm => 3,
        Format.R8G8B8SNorm => 3,
        Format.R8G8B8Uscaled => 3,
        Format.R8G8B8Sscaled => 3,
        Format.R8G8B8Uint => 3,
        Format.R8G8B8Sint => 3,
        Format.R8G8B8Srgb => 3,
        Format.B8G8R8Unorm => 3,
        Format.B8G8R8SNorm => 3,
        Format.B8G8R8Uscaled => 3,
        Format.B8G8R8Sscaled => 3,
        Format.B8G8R8Uint => 3,
        Format.B8G8R8Sint => 3,
        Format.B8G8R8Srgb => 3,
        Format.R8G8B8A8Unorm => 4,
        Format.R8G8B8A8SNorm => 4,
        Format.R8G8B8A8Uscaled => 4,
        Format.R8G8B8A8Sscaled => 4,
        Format.R8G8B8A8Uint => 4,
        Format.R8G8B8A8Sint => 4,
        Format.R8G8B8A8Srgb => 4,
        Format.B8G8R8A8Unorm => 4,
        Format.B8G8R8A8SNorm => 4,
        Format.B8G8R8A8Uscaled => 4,
        Format.B8G8R8A8Sscaled => 4,
        Format.B8G8R8A8Uint => 4,
        Format.B8G8R8A8Sint => 4,
        Format.B8G8R8A8Srgb => 4,
        Format.R32G32B32Uint => 12,
        Format.R32G32B32Sint => 12,
        Format.R32G32B32Sfloat => 12,
        Format.R32G32B32A32Uint => 16,
        Format.R32G32B32A32Sint => 16,
        Format.R32G32B32A32Sfloat => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Format currently unsupported!")
    };
}
