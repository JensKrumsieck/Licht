using System.Reflection;
using SkiaSharp;

namespace Catalyze.Tools;

public class FileTool
{
    public static byte[] ReadBytesFromResource(string filename)
    {
        var asm = Assembly.GetCallingAssembly();
        var resName = asm.GetName().Name + "." + filename; 
        if(!asm.GetManifestResourceNames().Contains(resName))
            throw new ApplicationException($"Could not find resource for {filename}");
        using var stream = asm.GetManifestResourceStream(resName);
        using var ms = new MemoryStream();
        if (stream is null) return Array.Empty<byte>();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

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
