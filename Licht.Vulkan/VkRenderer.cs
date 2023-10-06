using Licht.Core.Graphics;
using Microsoft.Extensions.Logging;

namespace Licht.Vulkan;

public class VkRenderer : IRenderer
{
    private readonly ILogger _logger;
    private VkGraphicsDevice _device;
    
    public VkRenderer(ILogger logger)
    {
        _logger = logger;
        _device = new VkGraphicsDevice(_logger);
    }
    
    public void BeginFrame()
    {
        
    }
    public void EndFrame()
    {
        
    }
}
