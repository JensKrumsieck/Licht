global using static Licht.Vulkan.Instance;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public partial class Instance
{
    public static readonly Vk vk = Vk.GetApi();
}
