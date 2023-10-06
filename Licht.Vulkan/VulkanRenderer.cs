using Licht.Core;
using Licht.Graphics;

namespace Licht.Vulkan;

public class VulkanRenderer : IRenderer
{
    public VulkanRenderer(ILogger logger)
    {
        InitializeLogging(logger);
        var context = new VkContext();
    }
    
    public void BeginFrame()
    {
        throw new NotImplementedException();
    }
    public void EndFrame()
    {
        throw new NotImplementedException();
    }
}
