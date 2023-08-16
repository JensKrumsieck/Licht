using Silk.NET.Vulkan;

namespace Catalyst.Allocation;

public struct AllocatedImage
{
    public Image Image;
    public Allocation Allocation;
}