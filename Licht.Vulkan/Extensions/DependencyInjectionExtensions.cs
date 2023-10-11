using Licht.Vulkan;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddVulkanRenderer(this ServiceCollection collection)
    {
        collection.AddSingleton<VkSurface>();
        collection.AddSingleton<VkGraphicsDevice>();
        collection.AddSingleton<VkRenderer>();
    }
}
