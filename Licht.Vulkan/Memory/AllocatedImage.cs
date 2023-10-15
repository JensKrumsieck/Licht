using Silk.NET.Vulkan;

namespace Licht.Vulkan.Memory;

public struct AllocatedImage
{
    public readonly Image Image;
    public readonly Allocation Allocation;
    
    internal AllocatedImage(Image image, Allocation allocation)
    {
        Image = image;
        Allocation = allocation;
    }
}