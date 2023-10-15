using Licht.Vulkan;
using Licht.Vulkan.Memory;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddVulkanRenderer<TAllocator>(this ServiceCollection collection) where TAllocator : class, IAllocator
    {
        collection.AddSingleton<VkSurface>();
        collection.AddSingleton<VkGraphicsDevice>();
        collection.AddSingleton<VkRenderer>();
        collection.AddSingleton<IAllocator, TAllocator>();
    }
}
