using Licht.Core;
using Licht.Core.Graphics;
using Microsoft.Extensions.Logging;

namespace Licht.Vulkan;

public class VulkanRenderer : IRenderer
{
    private VulkanDevice _context;
    public VulkanRenderer(ILogger logger)
    {
        InitializeLogging(logger);
        _context = new VulkanDevice();
    }
    
    public void BeginFrame()
    {
        
    }
    public void EndFrame()
    {
        
    }
}
