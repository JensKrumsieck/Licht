using SkiaSharp;

namespace Licht.Vulkan.Extensions;

public static class IOHelper
{
    public static SKBitmap LoadImageFromFile(string filename, SKColorType colorType)
    {
        using var fs = File.Open(filename, FileMode.Open);
        using var codec = SKCodec.Create(fs);
        var imgInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, colorType, SKAlphaType.Premul);
        var image = SKBitmap.Decode(codec, imgInfo);
        if (image is null) throw new IOException($"Failed to load image from {filename}");
        return image;
    }
}
