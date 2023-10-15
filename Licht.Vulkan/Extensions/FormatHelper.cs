using System.Numerics;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Licht.Vulkan.Extensions;

public static class FormatHelper
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

    public static Format ToVkFormat(this SKColorType colorType, bool preferSrgb = true) => colorType switch
    {
        SKColorType.Unknown => Format.Undefined,
        SKColorType.Rgb565 => Format.R5G6B5UnormPack16,
        SKColorType.Argb4444 => Format.A4R4G4B4UnormPack16,
        SKColorType.Rgba8888 => preferSrgb ? Format.R8G8B8A8Srgb : Format.R8G8B8A8Unorm,
        SKColorType.Bgra8888 => preferSrgb ? Format.B8G8R8A8Srgb : Format.B8G8R8A8Unorm,
        SKColorType.RgbaF16 => Format.R16G16B16A16Sfloat,
        SKColorType.RgbaF32 => Format.R32G32B32A32Uint,
        SKColorType.Rgba16161616 => Format.R16G16B16A16Unorm,
        _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType,
            "This conversion from SkColorType to VkFormat is not (yet) supported!")
    };
    
    public static SKColorType ToSkFormat(this Format format) => format switch
    {
        Format.Undefined => SKColorType.Unknown,
        Format.R5G6B5UnormPack16 => SKColorType.Rgb565,
        Format.A4R4G4B4UnormPack16 => SKColorType.Argb4444,
        Format.R8G8B8A8Srgb => SKColorType.Rgba8888,
        Format.R8G8B8A8Unorm => SKColorType.Rgba8888,
        Format.B8G8R8A8Srgb => SKColorType.Bgra8888,
        Format.B8G8R8A8Unorm => SKColorType.Bgra8888,
        Format.R16G16B16A16Sfloat => SKColorType.RgbaF16,
        Format.R32G32B32A32Uint => SKColorType.RgbaF32,
        Format.R16G16B16A16Unorm => SKColorType.Rgba16161616,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format,
            "This conversion from VkFormat to SkColorType is not (yet) supported!")
    };
}
